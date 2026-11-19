using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Lifecycles;

public partial class LifecycleTests : IntegrationTest, IDisposable
{
    [Fact]
    public async Task Enforces_initial_state_and_allowed_transitions()
    {
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var invalidCreate = 
            await client.PostAsJsonAsync("/api/task",
                new
                {
                    title = "Work",
                    status = "done"
                }, 
                ct);

        invalidCreate.StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);

        var created = 
            await client.PostAsJsonAsync("/api/task", 
                new
                {
                    title = "Work"
                }, 
                ct);
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var record =
            await created.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        record.GetProperty("status")
            .GetString()
            .Should()
            .Be("draft");

        var id = record.GetProperty("id").GetString();

        var invalid = 
            await client.PatchAsJsonAsync($"/api/task/{id}", 
                new
                {
                    status = "done"
                },
                ct);

        invalid.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var body = 
            await invalid.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        body.GetProperty("code").GetString().Should().Be("MTPR-2012");

        (await client.PatchAsJsonAsync($"/api/task/{id}",
            new
            {
                status = "review"
            }, ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        (await client.PatchAsJsonAsync($"/api/task/{id}", 
            new
            {
                title = "Updated"
            }, ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        (await client.PutAsJsonAsync($"/api/task/{id}", 
            new
            {
                title = "Updated", 
                status = "done"
            }, 
            ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        (await client.PatchAsJsonAsync($"/api/task/{id}", 
            new
            {
                status = "missing"
            }, 
            ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);

        var final =
            await client.GetFromJsonAsync<JsonElement>($"/api/task/{id}", ct);
        final.GetProperty("status")
            .GetString()
            .Should()
            .Be("done");
    }

    [Fact]
    public async Task Concurrent_transitions_are_checked_against_the_committed_state()
    {
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        var created = 
            await client.PostAsJsonAsync("/api/task",
                new { title = "Work" },
                ct);

        var id = 
            (await created.Content.
                ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("id")
            .GetString();

        var responses = await Task.WhenAll(
            client.PatchAsJsonAsync($"/api/task/{id}",
                new
                {
                    status = "review"
                }, 
                ct),
            client.PatchAsJsonAsync($"/api/task/{id}",
                new
                {
                    status = "cancelled"
                },
                ct));

        responses.Select(response => response.StatusCode)
            .Should()
            .BeEquivalentTo(
                [
                    HttpStatusCode.OK,
                    HttpStatusCode.UnprocessableEntity
                ]);
    }

    [Fact]
    public async Task Changed_configuration_controls_subsequent_transitions()
    {
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var created =
            await client.PostAsJsonAsync("/api/task", 
                new
                {
                    title = "Work"
                }, 
                ct);

        var id = 
            (await created.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("id")
            .GetString();

        (await client.PatchAsJsonAsync($"/api/task/{id}",
            new
            {
                status = "done"
            }, 
            ct)).
            StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);

        factory.Define("task", 
            definition.Replace(
                "\"from\": \"review\", \"to\": \"done\"", "\"from\": \"draft\", \"to\": \"done\""));

        (await client.PatchAsJsonAsync($"/api/task/{id}", 
            new
            {
                status = "done"
            },
            ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_rejecting_after_rule_rolls_back_a_valid_transition()
    {
        factory.Define("task", 
            definition.Replace("\"lifecycle\": {",
                """
            "rules": [{ "trigger": "afterUpdate",
              "condition": { "operator": "equal", "left": { "field": "status" }, "right": { "value": "review" } },
              "action": { "type": "reject", "message": "not yet" } }], "lifecycle": {
            """));

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var created = 
            await client.PostAsJsonAsync("/api/task",
                new
                {
                    title = "Work"
                }, 
                ct);

        var id = 
            (await created.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("id")
            .GetString();

        (await client.PatchAsJsonAsync($"/api/task/{id}",
            new
            {
                status = "review"
            }, ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);

        var record = 
            await client
                .GetFromJsonAsync<JsonElement>($"/api/task/{id}", 
                    ct);

        record.GetProperty("status")
            .GetString()
            .Should()
            .Be("draft");
    }
    [Theory]
    [InlineData("\"field\":\"missing\",\"initial\":\"draft\",\"states\":[\"draft\"],\"transitions\":[]")]
    [InlineData("\"field\":\"status\",\"initial\":\"missing\",\"states\":[\"draft\"],\"transitions\":[]")]
    [InlineData("\"field\":\"status\",\"initial\":\"draft\",\"states\":[\"draft\"],\"transitions\":[{\"from\":\"draft\",\"to\":\"missing\"}]")]
    [InlineData("\"field\":\"status\",\"initial\":\"draft\",\"states\":[\"draft\",\"draft\"],\"transitions\":[]")]
    public async Task Invalid_lifecycle_rejects_definition(string lifecycle)
    {
        factory.Define("task", $$"""
            { "name": "task", "fields": [{ "name": "status", "type": "string", "required": true }],
              "lifecycle": { {{lifecycle}} } }
            """);

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var response = await client.PostAsJsonAsync("/api/task",
            new { }, 
            ct);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.InternalServerError);

        var body = await response.Content
            .ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        body.GetProperty("code").GetString().Should().Be("MTPR-3001");
    }

    [Theory]
    [InlineData("{\"name\":\"status\",\"type\":\"string\"}")]
    [InlineData("{\"name\":\"status\",\"type\":\"string\",\"required\":true,\"default\":\"other\"}")]
    [InlineData("{\"name\":\"status\",\"type\":\"string\",\"required\":true,\"maxLength\":2}")]
    public async Task Lifecycle_requires_a_compatible_field(string field)
    {
        factory.Define("task", $$"""
            { "name": "task", "fields": [{{field}}],
              "lifecycle": { "field": "status", "initial": "draft", "states": ["draft"], "transitions": [] } }
            """);

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        var response = await client.PostAsJsonAsync("/api/task",
            new { }, 
            ct);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.InternalServerError);

        var body = await response.Content
            .ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        body.GetProperty("code")
            .GetString()
            .Should()
            .Be("MTPR-3001");
    }
}