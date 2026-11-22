using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security.RowFilters;

public class RowFilterReloadTests : IntegrationTest
{
    [Fact]
    public async Task Changed_row_filter_applies_without_restart()
    {
        await using MtprApiFactory factory = new();
        factory.Define("note", TestDoublesRowFilter.Definition);
        factory.ExtraClaims.Add(new Claim("tenant", "a"));
        factory.AdditionalConfiguration[TestDoublesRowFilter.Key] = "tenant:eq:a";

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

        factory.Services
            .GetRequiredService<IConfiguration>()[TestDoublesRowFilter.Key] = 
                "tenant:eq:b";

        ((IConfigurationRoot)factory
            .Services
            .GetRequiredService<IConfiguration>())
            .Reload();

        (await countAsync(client)).Should().Be(0);

        factory.Services
                .GetRequiredService<IConfiguration>()[TestDoublesRowFilter.Key] =
            "tenant:eq:{claim:tenant}";

        ((IConfigurationRoot)factory
                .Services.GetRequiredService<IConfiguration>())
            .Reload();

        (await countAsync(client)).Should().Be(1);

        factory.Services
                .GetRequiredService<IConfiguration>()[TestDoublesRowFilter.Key] =
            "tenant:unknown:b";

        ((IConfigurationRoot)factory
            .Services
            .GetRequiredService<IConfiguration>())
            .Reload();

        (await countAsync(client)).Should().Be(1);
    }

    [Fact]
    public async Task Invalid_reload_keeps_the_previous_policy()
    {
        await using MtprApiFactory factory = new();
        factory.Define("note", TestDoublesRowFilter.Definition);
        factory.AdditionalConfiguration[TestDoublesRowFilter.Key] = "tenant:eq:b";

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        await client.PostAsJsonAsync("/api/note", new { title = "x", tenant = "b" }, ct);

        factory.Services
            .GetRequiredService<IConfiguration>()[TestDoublesRowFilter.Key] =
                "not-a-filter";

        ((IConfigurationRoot)factory
            .Services
            .GetRequiredService<IConfiguration>())
            .Reload();

        (await countAsync(client)).Should().Be(1);
    }

    private static async Task<int> countAsync(HttpClient client)
    {
        var ct = TestContext.Current.CancellationToken;
        using var response = await client.GetAsync("/api/note", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        return list.GetArrayLength();
    }
}
