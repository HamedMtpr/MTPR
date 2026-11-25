using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.HelpdeskSample;

public partial class HelpdeskSampleTests : IntegrationTest, IDisposable
{
    [Fact]
    public async Task Documented_definitions_support_the_helpdesk_flow()
    {
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var customerId = await create(client, "customer",
            new { name = "Ada", email = "ada@example.com" });

        var ticket =
            await client.PostAsJsonAsync("/api/ticket",
                new { customerId, subject = "Cannot log in" },
                ct);

        ticket.StatusCode.Should().Be(HttpStatusCode.Created);

        var record =
            await ticket.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        record.GetProperty("status").GetString().Should().Be("open");
        record.GetProperty("priority").GetInt32().Should().Be(2);
        record.GetProperty("cost").GetDecimal().Should().Be(40m);

        var ticketId = record.GetProperty("id").GetString();

        var skipped =
            await client.PatchAsJsonAsync($"/api/ticket/{ticketId}",
                new { status = "closed" },
                ct);

        skipped.StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);

        var started =
            await client.PatchAsJsonAsync($"/api/ticket/{ticketId}",
                new { status = "inProgress" },
                ct);

        started.StatusCode.Should().Be(HttpStatusCode.OK);

        var comment =
            await client.PostAsJsonAsync("/api/ticketcomment",
                new { ticketId, body = "Looking into it" },
                ct);

        comment.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Rejects_tickets_estimated_above_forty_hours()
    {
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var customerId =
            await create(client, "customer",
            new { name = "Bob", email = "bob@example.com" });

        var response =
            await client.PostAsJsonAsync("/api/ticket",
                new { customerId, subject = "Rebuild", hours = 41 },
                ct);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejects_deleting_a_customer_that_has_tickets()
    {
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var customerId =
            await create(client, "customer",
            new { name = "Eve", email = "eve@example.com" });

        await create(client,
            "ticket",
            new { customerId, subject = "Help" });

        var response =
            await client.DeleteAsync(
                $"/api/customer/{customerId}",
                ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
