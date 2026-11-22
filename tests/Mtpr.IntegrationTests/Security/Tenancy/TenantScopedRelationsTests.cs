using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security.Tenancy;

public class TenantScopedRelationsTests : IntegrationTest
{
    [Fact]
    public async Task References_and_cascades_are_limited_to_the_same_tenant()
    {
        await using MtprApiFactory factory = new();
        factory.Define("parent", """
            {
              "name": "parent",
              "fields": [
                { "name": "tenant", "type": "string" }
              ]
            }
            """);
        factory.Define("child", """
            {
              "name": "child",
              "fields": [
                { "name": "tenant", "type": "string" },
                {
                  "name": "parentId",
                  "type": "guid",
                  "references": {
                    "entity": "parent",
                    "onDelete": "cascade"
                  }
                }
              ]
            }
            """);
        factory.AdditionalConfiguration["Mtpr:Tenancy:Entities:parent"] = "tenant";
        factory.AdditionalConfiguration["Mtpr:Tenancy:Entities:child"] = "tenant";
        factory.ExtraClaims.Add(new Claim("tenant_id", "a"));
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var parent = await client.PostAsJsonAsync("/api/parent", new { }, ct);
        parent.StatusCode.Should().Be(HttpStatusCode.Created);
        var parentId = (await parent.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString();

        using var child = await client.PostAsJsonAsync("/api/child",
            new { parentId }, ct);
        child.StatusCode.Should().Be(HttpStatusCode.Created);
        var childId = (await child.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString();

        factory.ExtraClaims.Clear();
        factory.ExtraClaims.Add(new Claim("tenant_id", "b"));
        using var foreign = await client.PostAsJsonAsync("/api/child",
            new { parentId }, ct);
        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var otherParent = await client.PostAsJsonAsync("/api/parent", new { }, ct);
        otherParent.StatusCode.Should().Be(HttpStatusCode.Created);
        var otherParentId = (await otherParent.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString();

        factory.ExtraClaims.Clear();
        factory.ExtraClaims.Add(new Claim("tenant_id", "a"));
        using var deniedUpdate = await client.PatchAsJsonAsync($"/api/child/{childId}",
            new { parentId = otherParentId }, ct);
        deniedUpdate.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await using SqliteConnection connection =
            new($"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO child (id, tenant, parentId) VALUES ($id, 'b', $parent)";
        var foreignId = Guid.NewGuid().ToString();
        command.Parameters.AddWithValue("$id", foreignId);
        command.Parameters.AddWithValue("$parent", parentId!);
        await command.ExecuteNonQueryAsync(ct);

        factory.ExtraClaims.Clear();
        factory.ExtraClaims.Add(new Claim("tenant_id", "a"));
        using var denied = await client.DeleteAsync($"/api/parent/{parentId}", ct);
        denied.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var stillThere = await client.GetAsync($"/api/parent/{parentId}", ct);
        stillThere.StatusCode.Should().Be(HttpStatusCode.OK);

        command.CommandText = "DELETE FROM child WHERE id = $id";
        await command.ExecuteNonQueryAsync(ct);

        using var deleted = await client.DeleteAsync($"/api/parent/{parentId}", ct);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var list = await client.GetAsync("/api/child", ct);
        (await list.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetArrayLength()
            .Should()
            .Be(0);
    }
}
