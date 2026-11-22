using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security.IndirectWrites;

public partial class IndirectWriteTests : IntegrationTest
{
    [Theory]
    [InlineData("cascade")]
    [InlineData("setNull")]
    public async Task Related_changes_require_entity_and_field_grants(string behavior)
    {
        await using MtprApiFactory factory = new() { GrantTestPermissions = false };
        factory.Define("parent", parent);
        factory.Define("child", child(behavior));
        factory.AdditionalConfiguration["Mtpr:Authorization:Roles:test-role:parent:0"] = "create";
        factory.AdditionalConfiguration["Mtpr:Authorization:Roles:test-role:parent:1"] = "read";
        factory.AdditionalConfiguration["Mtpr:Authorization:Roles:test-role:parent:2"] = "delete";
        factory.AdditionalConfiguration["Mtpr:Authorization:Roles:test-role:child:0"] = "create";
        factory.AdditionalConfiguration["Mtpr:Authorization:Roles:test-role:child:1"] = "read";
        if (behavior == "setNull")
            factory.AdditionalConfiguration["Mtpr:Authorization:Roles:test-role:child:2"] = "update";
        factory.AdditionalConfiguration["Mtpr:Authorization:Roles:manager:child:0"] = "delete";
        factory.AdditionalConfiguration["Mtpr:Authorization:Roles:manager:child:1"] = "update";
        factory.AdditionalConfiguration["Mtpr:Authorization:FieldWrites:child:manager:0"] = "*";
        factory.ExtraClaims.Add(new Claim(ClaimTypes.Role, "manager"));

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        var parentId = await createAsync(client, "parent",
            new
            {
                name = "parent"
            },
            ct);
        var childId = await createAsync(client, 
            "child",
            new
            {
                parentId
            }, 
            ct);
        factory.ExtraClaims.Clear();

        using var denied = 
            await client.DeleteAsync($"/api/parent/{parentId}", ct);
        denied.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var parentAfter = 
            await client.GetAsync($"/api/parent/{parentId}", ct);
        using var childAfter =
            await client.GetAsync($"/api/child/{childId}", ct);
        parentAfter.StatusCode.Should().Be(HttpStatusCode.OK);
        childAfter.StatusCode.Should().Be(HttpStatusCode.OK);

        (await childAfter.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("parentId")
            .GetString()
            .Should()
            .Be(parentId);

        factory.ExtraClaims.Add(new Claim(ClaimTypes.Role, "manager"));
        using var allowed = 
            await client.DeleteAsync($"/api/parent/{parentId}", ct);
        allowed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var finalChild = 
            await client.GetAsync($"/api/child/{childId}", ct);
        finalChild.StatusCode.Should().Be(behavior == "cascade"
            ? HttpStatusCode.NotFound
            : HttpStatusCode.OK);

        if (behavior == "setNull")
            (await finalChild.Content.ReadFromJsonAsync<JsonElement>(ct))
                .GetProperty("parentId")
                .ValueKind
                .Should()
                .Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Defaults_and_computed_changes_require_field_grants()
    {
        await using MtprApiFactory factory = new();
        factory.Define("line", line);
        factory.AdditionalConfiguration["Mtpr:Authorization:FieldWrites:line:test-role:0"] = "price";
        factory.AdditionalConfiguration["Mtpr:Authorization:FieldWrites:line:manager:0"] = "*";
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var deniedCreate =
            await client.PostAsJsonAsync("/api/line",
                new
                {
                    price = 2
                }, 
                ct);
        deniedCreate.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var list = 
            await client.GetAsync("/api/line", ct);
        (await list.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetArrayLength().Should().Be(0);

        await using (SqliteConnection connection = new(
                         $"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False"))
        {
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = 
                "SELECT detail FROM __mtpr_audit WHERE operation = 'denied'";
            (await command.ExecuteScalarAsync(ct))
                .Should()
                .Be("field-write:indirect");
        }

        factory.ExtraClaims.Add(new Claim(ClaimTypes.Role, "manager"));
        var id = await createAsync(client, "line", new
        {
            price = 2
        }, 
            ct);
        factory.ExtraClaims.Clear();

        using var deniedUpdate = 
            await client.PatchAsJsonAsync($"/api/line/{id}",
                new
                {
                    price = 3
                }, 
                ct);
        deniedUpdate.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var fetched = 
            await client.GetAsync($"/api/line/{id}", ct);
        var record = await fetched.Content.ReadFromJsonAsync<JsonElement>(ct);
        record.GetProperty("price").GetDecimal().Should().Be(2);
        record.GetProperty("total").GetDecimal().Should().Be(2);
    }

    private static async Task<string> createAsync(HttpClient client, string entity,
        object body, CancellationToken ct)
    {
        using var response =
            await client.PostAsJsonAsync($"/api/{entity}", body, ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString()!;
    }
}
