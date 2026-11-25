using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Mtpr.IntegrationTests.HelpdeskSample;

public partial class HelpdeskSampleTests
{
    private readonly MtprApiFactory factory = new();

    public HelpdeskSampleTests()
    {
        foreach (var file in Directory.GetFiles(
                     Path.Combine(AppContext.BaseDirectory, "m5-helpdesk"),
                     "*.json"))
            factory.Define(Path.GetFileNameWithoutExtension(file),
                File.ReadAllText(file));
    }

    private static async Task<string> create(HttpClient client, string entity, object body)
    {
        var response = await client.PostAsJsonAsync($"/api/{entity}",
            body,
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: TestContext.Current.CancellationToken))
            .GetProperty("id")
            .GetString()!;
    }

    public void Dispose()
    {
        factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
