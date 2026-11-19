using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.TransactionalRules;

public partial class TransactionalRuleTests : IntegrationTest, IDisposable
{
    [Theory]
    [InlineData("beforeCreate")]
    [InlineData("afterCreate")]
    public async Task Create_rejection_does_not_persist(string trigger)
    {
        define(trigger);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        
        var response = await client.PostAsJsonAsync("/api/task", 
            new
            {
                status = "locked", 
                priority = 1
            }, 
            ct);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);

        var rows =
            await client.GetFromJsonAsync<JsonElement>("/api/task", ct);

        rows.GetArrayLength().Should().Be(0);
    }

    [Theory]
    [InlineData("beforeUpdate")]
    [InlineData("afterUpdate")]
    public async Task Update_rules_use_merged_record_and_rejection_rolls_back(string trigger)
    {
        define(trigger);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var created = 
            await client.PostAsJsonAsync("/api/task", 
                new
                {
                    status = "locked", 
                    priority = 1
                }, 
                ct);

        var id = (await created.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("id")
            .GetString();

        var response = await client.PatchAsJsonAsync($"/api/task/{id}",
            new
            {
                priority = 2
            }, 
            ct);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);

        var record = 
            await client.GetFromJsonAsync<JsonElement>($"/api/task/{id}", ct);
        record.GetProperty("priority").GetInt32().Should().Be(1);
    }

    [Theory]
    [InlineData("beforeDelete")]
    [InlineData("afterDelete")]
    public async Task Delete_rejection_preserves_record(string trigger)
    {
        define(trigger);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var created = 
            await client.PostAsJsonAsync("/api/task", 
            new
            {
                status = "locked", 
                priority = 1
            },
            ct);

        var id = 
            (await created.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("id")
            .GetString();

        var response = await client.DeleteAsync($"/api/task/{id}", ct);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);

        (await client.GetAsync($"/api/task/{id}", ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Rejected_after_delete_also_rolls_back_cascade()
    {
        define("afterDelete");
        factory.Define("note", """
            { "name": "note", "fields": [
              { "name": "taskId", "type": "guid", "required": true,
                "references": { "entity": "task", "onDelete": "cascade" } }
            ] }
            """);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var created = 
            await client.PostAsJsonAsync("/api/task", 
                new
                {
                    status = "locked", 
                    priority = 1
                }, 
                ct);

        var id = (await created.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("id")
            .GetString();

        var note = await client
            .PostAsJsonAsync("/api/note",
                new
                {
                    taskId = id
                }, 
                ct);
        note.StatusCode.Should().Be(HttpStatusCode.Created);

        var noteId = (await note.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("id")
            .GetString();

        (await client.DeleteAsync($"/api/task/{id}", ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);

        (await client.GetAsync($"/api/note/{noteId}", ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }
    [Fact]
    public async Task Concurrent_updates_cannot_both_violate_a_combined_rule()
    {
        factory.Define("task", 
            $$"""
            { {{fields}}, "rules": [{ "trigger": "beforeUpdate",
              "condition": { "operator": "and", "conditions": [
                { "operator": "equal", "left": { "field": "status" }, "right": { "value": "locked" } },
                { "operator": "greaterThan", "left": { "field": "priority" }, "right": { "value": 0 } }
              ] }, "action": { "type": "reject", "message": "Invalid combination" } }] }
            """);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        var created = await client.PostAsJsonAsync("/api/task", 
            new
            {
                status = "open", 
                priority = 0
            }, 
            ct);

        var id = (await created.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("id")
            .GetString();

        var responses = await Task.WhenAll(
            client.PatchAsJsonAsync($"/api/task/{id}",
                new
                {
                    status = "locked"
                }, 
                ct),
            client.PatchAsJsonAsync($"/api/task/{id}",
                new
                {
                    priority = 1
                }, 
                ct));
        responses.Select(response => response.StatusCode)
            .Should()
            .BeEquivalentTo(
            [
                HttpStatusCode.OK,
                HttpStatusCode.UnprocessableEntity
            ]);
       
        var record = 
            await client.GetFromJsonAsync<JsonElement>($"/api/task/{id}", 
                ct);

        (record.GetProperty("status").GetString() == "locked" &&
         record.GetProperty("priority").GetInt32() == 1)
            .Should()
            .BeFalse();
    }
}


