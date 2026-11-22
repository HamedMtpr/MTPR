using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Mtpr.Engine.Definitions;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security.Tenancy;

public class TenantDefinitionReloadTests : IntegrationTest
{
    [Fact]
    public async Task Definition_reload_waits_until_active_catalog_lease_is_released()
    {
        await using var factory = TestDoublesTenancy.CreateFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var initial = await client.GetAsync("/api/note", ct);
        initial.StatusCode.Should().Be(HttpStatusCode.OK);

        var catalog = factory.Services.GetRequiredService<IDefinitionCatalog>();
        using var lease = await catalog.AcquireReadLeaseAsync(ct);
        using var pinned = catalog.PinSnapshot();

        factory.Define("note", """
            {
              "name": "note",
              "fields": [
                { "name": "title", "type": "string" },
                { "name": "tenant", "type": "string" },
                { "name": "extra", "type": "string" }
              ]
            }
            """);

        var snapshot = await catalog.GetAsync("note", ct);
        snapshot.FindField("extra").Should().BeNull();

        var request = client.GetAsync("/api/note", ct);
        var completed = await Task.WhenAny(request, Task.Delay(250, ct));
        completed.Should().NotBeSameAs(request);

        pinned.Dispose();
        lease.Dispose();

        using var response = await request.WaitAsync(TimeSpan.FromSeconds(10), ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await catalog.GetAsync("note", ct);
        updated.FindField("extra").Should().NotBeNull();
    }
}
