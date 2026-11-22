using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security.Tenancy;

public class TenantIsolationTests : IntegrationTest
{
    [Fact]
    public async Task Create_stamps_the_callers_tenant()
    {
        await using var factory = TestDoublesTenancy.CreateFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var response =
            await client.PostAsJsonAsync("/api/note",
                new { title = "mine" },
                ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        created.GetProperty("tenant").GetString().Should().Be("a");
    }

    [Fact]
    public async Task Create_for_another_tenant_is_denied()
    {
        await using var factory = TestDoublesTenancy.CreateFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var response =
            await client.PostAsJsonAsync("/api/note",
                new
                {
                    title = "x",
                    tenant = "b"
                },
                ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_and_get_never_cross_tenants()
    {
        await using var factory = TestDoublesTenancy.CreateFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var created =
            await client.PostAsJsonAsync("/api/note",
                new { title = "mine" },
                ct);

        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString();

        TestDoublesTenancy.SwitchTo(factory, "b");

        using var list = await client.GetAsync("/api/note", ct);
        (await list.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetArrayLength()
            .Should()
            .Be(0);

        using var single = await client.GetAsync($"/api/note/{id}", ct);
        single.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var filtered =
            await client.GetAsync("/api/note?filter=tenant:eq:a", ct);
        (await filtered.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetArrayLength()
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task Update_and_delete_never_cross_tenants()
    {
        await using var factory = TestDoublesTenancy.CreateFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var created =
            await client.PostAsJsonAsync("/api/note",
                new { title = "mine" },
                ct);

        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString();

        TestDoublesTenancy.SwitchTo(factory, "b");

        using var patch =
            await client.PatchAsJsonAsync($"/api/note/{id}",
                new { title = "hijacked" },
                ct);
        patch.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var delete = await client.DeleteAsync(
            $"/api/note/{id}", 
            ct);
        delete.StatusCode.Should().Be(HttpStatusCode.NotFound);

        TestDoublesTenancy.SwitchTo(factory, "a");

        using var single = await client.GetAsync(
            $"/api/note/{id}", 
            ct);
        single.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_cannot_move_a_record_to_another_tenant()
    {
        await using var factory = TestDoublesTenancy.CreateFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var created =
            await client.PostAsJsonAsync("/api/note",
                new { title = "mine" },
                ct);

        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString();

        using var patch =
            await client.PatchAsJsonAsync($"/api/note/{id}",
                new { tenant = "b" },
                ct);

        patch.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Put_without_the_tenant_field_keeps_the_tenant()
    {
        await using var factory = TestDoublesTenancy.CreateFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var created =
            await client.PostAsJsonAsync("/api/note",
                new { title = "mine" },
                ct);

        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString();

        using var put =
            await client.PutAsJsonAsync($"/api/note/{id}",
                new { title = "changed" },
                ct);

        put.StatusCode.Should().Be(HttpStatusCode.OK);
        (await put.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("tenant")
            .GetString()
            .Should()
            .Be("a");
    }

    [Fact]
    public async Task Caller_without_a_tenant_claim_is_denied()
    {
        await using var factory = TestDoublesTenancy.CreateFactory();
        TestDoublesTenancy.SwitchTo(factory, null);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var list =
            await client.GetAsync("/api/note", ct);
        list.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var create =
            await client.PostAsJsonAsync("/api/note",
                new { title = "x" },
                ct);
        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Tenancy_applies_without_restart()
    {
        await using var factory = TestDoublesTenancy.CreateFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var created =
            await client.PostAsJsonAsync("/api/note",
                new { title = "mine" },
                ct);

        TestDoublesTenancy.SwitchTo(factory, "b");

        using var isolated = 
            await client.GetAsync("/api/note", ct);
        (await isolated.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetArrayLength()
            .Should()
            .Be(0);

        var configuration =
            factory.Services.GetRequiredService<IConfiguration>();

        configuration[TestDoublesTenancy.EntityKey] = null;
        ((IConfigurationRoot)configuration).Reload();

        using var open =
            await client.GetAsync("/api/note", ct);
        (await open.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetArrayLength()
            .Should()
            .Be(1);
    }
}
