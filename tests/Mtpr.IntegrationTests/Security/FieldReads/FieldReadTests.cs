using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security.FieldReads;

public partial class FieldReadTests : IntegrationTest
{
    [Fact]
    public async Task Ungranted_fields_are_removed_from_all_record_responses()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        factory.AdditionalConfiguration[
            "Mtpr:Authorization:FieldReads:product:test-role:0"] = "title";

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var created = 
            await client.PostAsJsonAsync("/api/product",
                new { title = "Pen", secret = "private" },
                ct);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>(ct);
        body.TryGetProperty("secret", out _).Should().BeFalse();
        body.GetProperty("title").GetString().Should().Be("Pen");

        var id = body.GetProperty("id").GetString();

        using var fetched =
            await client.GetAsync($"/api/product/{id}", ct);

        var record = await fetched.Content.ReadFromJsonAsync<JsonElement>(ct);
        record.TryGetProperty("secret", out _).Should().BeFalse();

        using var listed =
            await client.GetAsync("/api/product", ct);

        var list =
            await listed.Content.ReadFromJsonAsync<JsonElement>(ct);
        list[0].TryGetProperty("secret", out _).Should().BeFalse();

        using var patched = 
            await client.PatchAsJsonAsync($"/api/product/{id}",
                new { title = "Paper" },
                ct);

        var updated = await patched.Content.ReadFromJsonAsync<JsonElement>(ct);
        updated.TryGetProperty("secret", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("fields=secret")]
    [InlineData("sort=-secret")]
    [InlineData("filter=secret:eq:private")]
    public async Task Query_on_ungranted_field_is_rejected(string query)
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        factory.AdditionalConfiguration[
            "Mtpr:Authorization:FieldReads:product:test-role:0"] = "title";

        using var client = factory.CreateClient();
        using var response = 
            await client.GetAsync($"/api/product?{query}",
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Granted_fields_and_id_can_be_queried()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        factory.AdditionalConfiguration[
            "Mtpr:Authorization:FieldReads:product:test-role:0"] = "title";

        using var client = factory.CreateClient();
        using var response = 
            await client.GetAsync("/api/product?fields=id,title&sort=-title&filter=title:eq:Pen",
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Wildcard_grant_exposes_every_field()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        factory.AdditionalConfiguration[
            "Mtpr:Authorization:FieldReads:product:test-role:0"] = "*";

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var created = 
            await client.PostAsJsonAsync("/api/product",
                new { title = "Pen", secret = "private" },
                ct);

        var body = await created.Content.ReadFromJsonAsync<JsonElement>(ct);
        body.GetProperty("secret").GetString().Should().Be("private");
    }

    [Fact]
    public async Task Grant_for_another_role_denies_list_queries()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        factory.AdditionalConfiguration[
            "Mtpr:Authorization:FieldReads:product:other-role:0"] = "*";

        using var client = factory.CreateClient();
        using var response = 
            await client.GetAsync("/api/product",
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}