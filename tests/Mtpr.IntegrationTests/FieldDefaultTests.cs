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