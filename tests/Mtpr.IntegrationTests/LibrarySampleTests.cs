using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests;

public class LibrarySampleTests : IntegrationTest
{
    [Fact]
    public async Task Documented_definitions_support_the_library_flow()
    {
        await using var factory = new MtprApiFactory();

        foreach (var file in Directory.GetFiles(
                     Path.Combine(AppContext.BaseDirectory, "m2-library"),
                     "*.json"))
            factory.Define(Path.GetFileNameWithoutExtension(file),
                await File.ReadAllTextAsync(file,
                        TestContext.Current.CancellationToken));

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        async Task<string> create(string entity, object body)
        {
            var response = await client.PostAsJsonAsync($"/api/{entity}",
                body,
                cancellationToken: ct);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: ct))
                .GetProperty("id")
                .GetString()!;
        }

        var authorId = await create("author", new { name = "Ada" });

        var bookId = await create("book", new { title = "Algorithms",
            price = 12.5m,
            authorId });

        var tagId = await create("tag", new { name = "computing" });

        await create("booktag", new { bookId, tagId });
        var books = await client.GetFromJsonAsync<JsonElement>(
            $"/api/book?filter=authorId:eq:{authorId}",
            cancellationToken: ct);
        books.GetArrayLength().Should().Be(1);

        var links = await client.GetFromJsonAsync<JsonElement>(
            $"/api/booktag?filter=bookId:eq:{bookId}",
            cancellationToken: ct);
        links.GetArrayLength().Should().Be(1);

        (await client.DeleteAsync($"/api/author/{authorId}", ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);

        (await client.DeleteAsync($"/api/book/{bookId}", ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        (await client.GetFromJsonAsync<JsonElement>(
                $"/api/booktag?filter=bookId:eq:{bookId}",
                cancellationToken: ct))
            .GetArrayLength()
            .Should()
            .Be(0);

        (await client.GetAsync($"/api/tag/{tagId}", ct)).
            StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }
}
