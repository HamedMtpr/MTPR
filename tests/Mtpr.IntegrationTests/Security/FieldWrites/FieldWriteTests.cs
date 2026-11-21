using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security.FieldWrites;

public partial class FieldWriteTests : IntegrationTest
{
    [Fact]
    public async Task Restricted_fields_are_rejected_and_put_requires_full_access()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        factory.AdditionalConfiguration[
            "Mtpr:Authorization:FieldWrites:product:test-role:0"] = "title";

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var deniedCreate = 
            await client.PostAsJsonAsync("/api/product", 
                new
                {
                    title = "Pen", 
                    secret = "private"
                }, 
                ct);

        deniedCreate.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var created = await client.PostAsJsonAsync("/api/product",
            new { TITLE = "Pen" },
            ct);

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString();

        using var deniedPatch =
            await client.PatchAsJsonAsync($"/api/product/{id}", 
                new { SeCrEt = "private" }, 
                ct);

        deniedPatch.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var deniedPut =
            await client.PutAsJsonAsync($"/api/product/{id}", 
                new { title = "Paper" }, 
                ct);

        deniedPut.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var allowedPatch =
            await client.PatchAsJsonAsync($"/api/product/{id}", 
                new { title = "Paper" }, 
                ct);

        allowedPatch.StatusCode.Should().Be(HttpStatusCode.OK);

        using var fetched = 
            await client.GetAsync($"/api/product/{id}", ct);

        var record = await fetched.Content.ReadFromJsonAsync<JsonElement>(ct);
        record.GetProperty("title").GetString().Should().Be("Paper");
        record.GetProperty("secret").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Wildcard_field_grant_allows_full_replacement()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        factory.AdditionalConfiguration[
            "Mtpr:Authorization:FieldWrites:product:test-role:0"] = "*";

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        using var created = 
            await client.PostAsJsonAsync("/api/product", 
                new
                {
                    title = "Pen", 
                    secret = "private"
                }, 
                ct);

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var id = 
            (await created.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString();

        using var replaced = 
            await client.PutAsJsonAsync($"/api/product/{id}", 
                new { title = "Paper" }, 
                ct);

        replaced.StatusCode.Should().Be(HttpStatusCode.OK);
        var record = await replaced.Content.ReadFromJsonAsync<JsonElement>(ct);
        record.GetProperty("secret").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Field_grant_for_another_role_does_not_authorize_input()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        factory.AdditionalConfiguration[
            "Mtpr:Authorization:FieldWrites:product:other-role:0"] = "*";

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/product",
            new { title = "Pen" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var empty =
            await client.PostAsJsonAsync("/api/product", 
                new { }, 
                TestContext.Current.CancellationToken);

        empty.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Non_object_body_is_rejected_for_configured_entity()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        factory.AdditionalConfiguration[
            "Mtpr:Authorization:FieldWrites:product:test-role:0"] = "*";

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/product",
            new[] { "title" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
