using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.CreateRules;

public partial class CreateRuleTests : IntegrationTest, IDisposable
{
    private readonly MtprApiFactory factory = new();

    [Fact]
    public async Task Rules_apply_to_create_and_reload_without_rebuild()
    {
        factory.Define("task", definition);
        using var client = factory.CreateClient();
        var token = TestContext.Current.CancellationToken;
        var rejected = await client.PostAsJsonAsync("/api/task", 
            new
            {
                priority = 6,
                status = "open"
            }, 
            token);
        rejected.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var error = await rejected.Content
            .ReadFromJsonAsync<JsonElement>(cancellationToken: token);
        error.GetProperty("code").GetString().Should().Be("MTPR-2011");

        var accepted = await client.PostAsJsonAsync("/api/task",
            new { status = "open" },
            token);
        accepted.StatusCode.Should().Be(HttpStatusCode.Created);

        var rows = await client.GetFromJsonAsync<JsonElement>("/api/task", token);
        rows.GetArrayLength().Should().Be(1);

        factory.Define("task", 
            definition.Replace("\"value\": 5", "\"value\": 1"));
        var afterChange = await client.PostAsJsonAsync("/api/task", 
            new { status = "open" },
            token);
        afterChange.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData("{\"operator\":\"unknown\",\"left\":{\"field\":\"priority\"},\"right\":{\"value\":1}}")]
    [InlineData("{\"operator\":\"equal\",\"left\":{\"field\":\"missing\"},\"right\":{\"value\":1}}")]
    [InlineData("{\"operator\":\"equal\",\"left\":{\"field\":\"priority\"},\"right\":{\"value\":\"wrong\"}}")]
    [InlineData("{\"operator\":\"equal\",\"left\":{\"field\":\"priority\"},\"right\":{\"value\":1},\"extra\":true}")]
    public async Task Invalid_conditions_reject_the_definition(string condition)
    {
        factory.Define("task", $$"""
            { "name": "task", "fields": [{ "name": "priority", "type": "int" }],
              "rules": [{ "trigger": "beforeCreate", "condition": {{condition}},
                         "action": { "type": "reject", "message": "invalid" } }] }
            """);
        using var client = factory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var response = await client.PostAsJsonAsync("/api/task", 
            new { }, 
            token);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var error = await response.Content
            .ReadFromJsonAsync<JsonElement>(cancellationToken: token);

        error.GetProperty("code").GetString().Should().Be("MTPR-3001");
    }

    public void Dispose()
    {
        factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
