using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mtpr.Engine.Errors;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests;

public class VerticalSliceTests : IntegrationTest, IDisposable
{
    private const string product = """
        { 
            "name": "product", 
            "fields": [
          {     
            "name": "title", 
            "type": "string", 
            "required": true, 
            "minLength": 2, 
            "maxLength": 10 
          },
          { 
            "name": "price", 
            "type": "decimal", 
            "required": true, 
            "min": 0 
           } ] 
        }
        """;

    private readonly MtprApiFactory factory = new();
    private readonly HttpClient client;

    public VerticalSliceTests()
    {
        factory.Define("product", product);
        client = factory.CreateClient();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Crud_lifecycle_works_from_definition_only()
    {
        var created = 
            await client.PostAsJsonAsync("/api/product",
                new
                { 
                    title = "Pen", 
                    price = 1.5
                }, 
                cancellationToken: TestContext.Current.CancellationToken);

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: TestContext.Current.CancellationToken))
            .GetProperty("id")
            .GetString();

        var fetched = 
            await client.GetFromJsonAsync<JsonElement>($"/api/product/{id}",
                cancellationToken: TestContext.Current.CancellationToken);
        
        fetched.GetProperty("title")
            .GetString()
            .Should()
            .Be("Pen");

        var patched = await client.PatchAsJsonAsync($"/api/product/{id}",
            new
            {
                price = 2
            }, 
            cancellationToken: TestContext.Current.CancellationToken);

        (await patched.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: TestContext.Current.CancellationToken))
            .GetProperty("price")
            .GetDecimal()
            .Should()
            .Be(2);

        (await client.GetFromJsonAsync<JsonElement>("/api/product",
                cancellationToken: TestContext.Current.CancellationToken))
            .GetArrayLength()
            .Should()
            .Be(1);

        (await client.DeleteAsync($"/api/product/{id}",
                TestContext.Current.CancellationToken))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        (await client.GetAsync($"/api/product/{id}", 
                TestContext.Current.CancellationToken))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invalid_input_returns_all_violations()
    {
        var response = await client.PostAsJsonAsync("/api/product",
            new
            {
                title = "x",
                price = -1, 
                extra = 1
            },
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);
        
        body.GetProperty("errors").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task Unknown_entity_returns_404()
    {
        var response = await client.GetAsync("/api/ghost", 
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task New_definition_is_served_without_restart()
    {
        (await client.GetAsync("/api/product",
            TestContext.Current.CancellationToken))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
        
        factory.Define("tag", 
            """
            {
                "name": "tag",
                "fields":
                 [
                     {
                          "name": "label",
                          "type": "string",
                          "required": true 
                      } 
                  ] 
            }
            """);

        var response = await client.PostAsJsonAsync("/api/tag",
            new { label = "a" },
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Put_replaces_optional_fields_and_patch_preserves_them()
    {
        factory.Define("note",
            """
            { 
                "name":"note",
                 "fields":[
                 {
                    "name":"title",
                    "type":"string",
                    "required":true
                 },
                 {
                    "name":"body",
                    "type":"string"
                 }] 
            }
            """);
        var created = 
            await client.PostAsJsonAsync("/api/note", new
            {
                title = "one",
                body = "text"
            }, 
                cancellationToken: TestContext.Current.CancellationToken);

        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: TestContext.Current.CancellationToken))
            .GetProperty("id")
            .GetString();

        var patch = 
            await client.PatchAsJsonAsync($"/api/note/{id}", 
                new
                {
                    title = "two"
                }, 
                cancellationToken: TestContext.Current.CancellationToken);

        (await patch.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: TestContext.Current.CancellationToken))
            .GetProperty("body")
            .GetString()
            .Should().Be("text");

        var put = await client.PutAsJsonAsync($"/api/note/{id}", 
            new
            {
                title = "three"
            }, 
            cancellationToken: TestContext.Current.CancellationToken);

        (await put.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: TestContext.Current.CancellationToken))
            .GetProperty("body")
            .ValueKind
            .Should()
            .Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Duplicate_case_insensitive_fields_return_a_coded_error()
    {
        using var body = new StringContent(
            """
            {
                "title":"one",
                "Title":"two",
                "price":1
            }
            """, 
            System.Text.Encoding.UTF8, 
            "application/json");

        var response = await client.PostAsync("/api/product", body, 
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: TestContext.Current.CancellationToken))
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("MTPR-2008");
    }

    [Fact]
    public async Task Missing_field_type_is_rejected_as_invalid_definition()
    {
        factory.Define("bad", 
            """
            { 
                "name":"bad", 
                "fields":[
                {
                    "name":"value"
                }] 
            }
            """);
        var response = await client.GetAsync("/api/bad",
            TestContext.Current.CancellationToken);

        (await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: TestContext.Current.CancellationToken))
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("MTPR-3001");
    }

    [Fact]
    public async Task Adding_optional_fields_preserves_existing_records_across_multiple_migrations()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await client.PostAsJsonAsync("/api/product",
            new
            {
                title = "Pen",
                price = 2
            }, 
            cancellationToken: ct);

        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("id")
            .GetString();

        var first = product.Replace("\"min\": 0", 
            "\"min\": 0 }, { \"name\": \"note\", \"type\": \"string\"");

        factory.Define("product", first);

        var migrated = await client.GetFromJsonAsync<JsonElement>($"/api/product/{id}", 
            cancellationToken: ct);

        migrated.GetProperty("title")
            .GetString()
            .Should()
            .Be("Pen");

        migrated.GetProperty("note")
            .ValueKind
            .Should()
            .Be(JsonValueKind.Null);

        factory.Define("product",
            first.Replace("\"name\": \"note\", \"type\": \"string\"", 
                "\"name\": \"note\", \"type\": \"string\" }, { \"name\": \"code\", \"type\": \"int\""));

        var again = await client.GetFromJsonAsync<JsonElement>($"/api/product/{id}",
            cancellationToken: ct);

        again.GetProperty("code")
            .ValueKind
            .Should()
            .Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Adding_a_required_field_is_rejected_without_changing_the_database()
    {
        var ct = TestContext.Current.CancellationToken;
        await client.GetAsync("/api/product", ct);

        factory.Define("product", 
            product.Replace("\"min\": 0", "\"min\": 0 }, { \"name\": \"note\", \"type\": \"string\", \"required\": true"));

        (await client.GetAsync("/api/product", ct)).
            StatusCode.
            Should()
            .Be(HttpStatusCode.InternalServerError);

        factory.Define("product", product);

        (await client.GetAsync("/api/product", ct)).StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Changing_an_applied_definition_is_rejected()
    {
        await client.GetAsync("/api/product",
            TestContext.Current.CancellationToken);
        
        factory.Define("product", 
            product.Replace("\"maxLength\": 10", "\"maxLength\": 20"));

        var response = await client.GetAsync("/api/product", 
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
