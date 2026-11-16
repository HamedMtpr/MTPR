using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Mtpr.IntegrationTests.RelationsAndQueries;

public partial class RelationsAndQueryTests
{
    private readonly MtprApiFactory factory = new();
    private readonly HttpClient client;

    public RelationsAndQueryTests()
    {
        factory.Define("author",
            """
            {
               "name":"author",
               "fields":[
               {
                  "name":"name",
                  "type":"string",
                  "required":true
               }]
            }
            """);

        factory.Define("book",
            """
            {
               "name":"book",
               "fields":[
               {
                  "name":"title",
                  "type":"string",
                  "required":true
               },
               {
                  "name":"price",
                  "type":"decimal",
                  "required":true
               },
               {
                  "name":"authorId",
                  "type":"guid",
                  "references":
                  {
                     "entity":"author",
                     "onDelete":"restrict"
                  }
               }]
            }
            """);

        factory.Define("tag",
            """
            {
               "name":"tag",
               "fields":[
               {
                  "name":"name",
                  "type":"string",
                  "required":true
               }]
            }
            """);

        factory.Define("booktag",
            """
            {
               "name":"booktag",
               "fields":[
               {
                  "name":"bookId",
                  "type":"guid",
                  "required":true,
                  "references":
                  {
                     "entity":"book",
                     "onDelete":"cascade"
                  }
               },
               {
                  "name":"tagId",
                  "type":"guid",
                  "required":true,
                  "references":
                  {
                     "entity":"tag",
                     "onDelete":"cascade"
                  }
               }],
               "unique":[["bookId","tagId"]]
            }
            """);
        factory.Define("note",
            """
            {
               "name":"note",
               "fields":[
               {
                  "name":"body",
                  "type":"string"
               },
               {
                  "name":"authorId",
                  "type":"guid",
                  "references":
                  {
                     "entity":"author",
                     "onDelete":"setNull"
                  }
               }]
            }
            """);

        client = factory.CreateClient();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<string> create(string entity, object value)
    {
        var response = await client.PostAsJsonAsync($"/api/{entity}", value,
            cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken)).GetProperty("id").GetString()!;
    }
}