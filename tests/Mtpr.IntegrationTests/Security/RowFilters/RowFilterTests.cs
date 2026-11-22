using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security.RowFilters;

public class RowFilterTests : IntegrationTest
{
    [Fact]
    public async Task List_returns_only_rows_in_scope()
    {
        await using var factory = TestDoublesRowFilter.CreateFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var mine =
            await client.PostAsJsonAsync("/api/note",
                new
                {
                    title = "mine", 
                    tenant = "a"
                },
                ct);

        mine.StatusCode.Should().Be(HttpStatusCode.Created);

        factory.ExtraClaims.Clear();
        factory.ExtraClaims.Add(new Claim("tenant", "b"));
        
        using var other = 
            await client.PostAsJsonAsync("/api/note", 
                new
                {
                    title = "other", 
                    tenant = "b"
                }, 
                ct);

        other.StatusCode.Should().Be(HttpStatusCode.Created);

        factory.ExtraClaims.Clear();
        factory.ExtraClaims.Add(new Claim("tenant", "a"));

        using var response = await client.GetAsync("/api/note", ct);
        var list = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        list.GetArrayLength().Should().Be(1);
        list[0].GetProperty("title")
            .GetString()
            .Should()
            .Be("mine");
    }

    [Fact]
    public async Task Caller_filter_cannot_widen_the_scope()
    {
        await using var factory = TestDoublesRowFilter.CreateFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        factory.ExtraClaims.Clear();
        factory.ExtraClaims.Add(new Claim("tenant", "b"));
        using var created = 
            await client.PostAsJsonAsync("/api/note", 
                new
                {
                    title = "other", 
                    tenant = "b"
                },
                ct);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        factory.ExtraClaims.Clear();
        factory.ExtraClaims.Add(new Claim("tenant", "a"));

        using var response = 
            await client.GetAsync("/api/note?filter=tenant:eq:b", ct);
        var list = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        list.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Missing_claim_matches_no_rows()
    {
        await using MtprApiFactory factory = new();
        factory.Define("note", TestDoublesRowFilter.Definition);
        factory.AdditionalConfiguration[
                TestDoublesRowFilter.Key] =
                "tenant:eq:{claim:tenant}";

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var denied =
            await client.PostAsJsonAsync("/api/note",
                new
                {
                    title = "x", 
                    tenant = "a"
                }, 
                ct);
        denied.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var response = await client.GetAsync("/api/note", ct);
        var list = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        list.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Rows_outside_scope_are_not_found_for_get_patch_and_delete()
    {
        await using var factory = TestDoublesRowFilter.CreateFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        // Row is created while the caller is not scoped to it: use a different tenant claim.
        factory.ExtraClaims.Clear();
        factory.ExtraClaims.Add(new Claim("tenant", "b"));

        using var created = 
            await client.PostAsJsonAsync("/api/note",
                new
                {
                    title = "other",
                    tenant = "b"
                },
                ct);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id").GetString();

        factory.ExtraClaims.Clear();
        factory.ExtraClaims.Add(new Claim("tenant", "a"));

        (await client.GetAsync($"/api/note/{id}", ct))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await client.PatchAsJsonAsync($"/api/note/{id}", 
                new
                {
                    title = "x"
                },
                ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);

        (await client.DeleteAsync($"/api/note/{id}", ct))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Rows_in_scope_remain_accessible()
    {
        await using var factory = TestDoublesRowFilter.CreateFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var created = 
            await client.PostAsJsonAsync("/api/note",
                new
                {
                    title = "mine", 
                    tenant = "a"
                },
                ct);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString();

        (await client.GetAsync($"/api/note/{id}", ct))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.DeleteAsync($"/api/note/{id}", ct))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Write_that_moves_record_outside_scope_is_rolled_back()
    {
        await using var factory = TestDoublesRowFilter.CreateFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var created = 
            await client.PostAsJsonAsync("/api/note",
                new
                {
                    title = "mine", 
                    tenant = "a"
                },
                ct);

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString();

        using var updated = 
            await client.PatchAsJsonAsync($"/api/note/{id}",
                new { tenant = "b" },
                ct);

        updated.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var fetched =
            await client.GetAsync($"/api/note/{id}",
                ct);

        var record = await fetched.Content.ReadFromJsonAsync<JsonElement>(ct);
        record.GetProperty("tenant")
            .GetString()
            .Should()
            .Be("a");
    }

    [Fact]
    public async Task Filters_within_role_are_combined_and_roles_are_unioned()
    {
        await using var factory = TestDoublesRowFilter.CreateFactory();
        factory.AdditionalConfiguration[
            "Mtpr:Authorization:RowFilters:note:test-role:1"] = "title:eq:mine";
        factory.AdditionalConfiguration[
            "Mtpr:Authorization:RowFilters:note:other-role:0"] = "tenant:eq:b";
        factory.ExtraClaims.Add(new Claim(ClaimTypes.Role, "other-role"));

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var first = 
            await client.PostAsJsonAsync("/api/note",
                new
                {
                    title = "mine",
                    tenant = "a"
                }, 
                ct);
        using var second = 
            await client.PostAsJsonAsync("/api/note",
                new
                {
                    title = "other", tenant = "b"
                }, 
                ct);
        using var denied = 
            await client.PostAsJsonAsync("/api/note",
                new
                {
                    title = "other", tenant = "a"
                }, 
                ct);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        denied.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var response = await client.GetAsync("/api/note", ct);
        var list = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        list.GetArrayLength().Should().Be(2);
    }
}
