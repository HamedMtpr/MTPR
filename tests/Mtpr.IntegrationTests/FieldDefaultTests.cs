using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests;

public class FieldDefaultTests : IntegrationTest, IDisposable
{
    private readonly MtprApiFactory factory = new();

    [Fact]
    public async Task Defaults_apply_only_to_omitted_create_fields()
    {
        factory.Define("task", 
            """
            { 
               "name": "task",
               "fields": [
               {
                  "name": "status",
                  "type": "string", 
                  "required": true, 
                  "default": "new"
              },
              {
                 "name": "priority",
                 "type": "int", 
                 "default": 2 
              }]
            }
            """);
        using var client = factory.CreateClient();
        var token = TestContext.Current.CancellationToken;
        var response = await client.PostAsJsonAsync("/api/task", new { }, token);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: token);
        created.GetProperty("status").GetString().Should().Be("new");
        created.GetProperty("priority").GetInt32().Should().Be(2);
        var id = created.GetProperty("id").GetString();

        var updated = await client.PatchAsJsonAsync($"/api/task/{id}",
            new { status = "active" }
            , token);

        var record = await updated.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: token);
        record.GetProperty("status").GetString().Should().Be("active");
        record.GetProperty("priority").GetInt32().Should().Be(2);

        var explicitNull = await client.PostAsJsonAsync("/api/task", 
            new { status = (string?)null }, token);
        explicitNull.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Changed_default_is_used_without_rebuilding_and_does_not_modify_existing_records()
    {
        const string initial = """
            { "name": "task", "fields": [
              { "name": "priority", "type": "int", "default": 2 }
            ] }
            """;
        factory.Define("task", initial);
        using var client = factory.CreateClient();
        var token = TestContext.Current.CancellationToken;
        var first = await client.PostAsJsonAsync("/api/task", new { priority = 7 }, token);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstRecord = await first.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: token);
        var id = firstRecord.GetProperty("id").GetString();

        factory.Define("task", initial.Replace("\"default\": 2", "\"default\": 3"));
        var second = await client.PostAsJsonAsync("/api/task", new { }, token);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        var secondRecord = await second.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: token);
        secondRecord.GetProperty("priority").GetInt32().Should().Be(3);
        var patch = await client.PatchAsJsonAsync($"/api/task/{id}", new { }, token);
        patch.StatusCode.Should().Be(HttpStatusCode.OK);
        var unchanged = await patch.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: token);
        unchanged.GetProperty("priority").GetInt32().Should().Be(7);

        var explicitNull = await client.PostAsJsonAsync("/api/task", new { priority = (int?)null }, token);
        explicitNull.StatusCode.Should().Be(HttpStatusCode.Created);
        var nullRecord = await explicitNull.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: token);
        nullRecord.GetProperty("priority").ValueKind.Should().Be(JsonValueKind.Null);
    }
    [Theory]
    [InlineData("\"wrong\"")]
    [InlineData("11")]
    public async Task Invalid_defaults_reject_the_definition(string value)
    {
        factory.Define("task", $$"""
            { "name": "task", "fields": [
              { "name": "priority", "type": "int", "max": 10, "default": {{value}} }
            ] }
            """);
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/task", 
            new { },
            TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.Should().BeFalse();
    }

    public void Dispose()
    {
        factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
