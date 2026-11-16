using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.RelationsAndQueries;

public partial class RelationsAndQueryTests : IntegrationTest, IDisposable
{
    [Fact]
    public async Task Relations_enforce_restrict_cascade_set_null_and_unique_pairs()
    {
        var ct = TestContext.Current.CancellationToken;
        var author = await create("author", new { name = "Ada" });
        var book = await create("book", new { title = "One", price = 10, authorId = author });
        var tag = await create("tag", new { name = "science" });
        var link = await create("booktag", new { bookId = book, tagId = tag });
        var note = await create("note", new { body = "hello", authorId = author });

        (await client.PostAsJsonAsync("/api/booktag",
            new
            {
                bookId = book,
                tagId = tag
            }, cancellationToken: ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);

        (await client.PostAsJsonAsync("/api/book",
            new
            {
                title = "bad",
                price = 1,
                authorId = Guid.NewGuid()
            },
            cancellationToken: ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);

        (await client.DeleteAsync($"/api/author/{author}", ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);

        (await client.DeleteAsync($"/api/book/{book}", ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        (await client.GetAsync($"/api/booktag/{link}", ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);

        (await client.DeleteAsync($"/api/author/{author}", ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        var savedNote = await client.GetFromJsonAsync<JsonElement>($"/api/note/{note}",
            cancellationToken: ct);

        savedNote.GetProperty("authorId")
            .ValueKind
            .Should().Be(JsonValueKind.Null);
    }

    [Theory]
    [InlineData("""{"name":"broken","fields":[{"name":"parent","type":"string","references":{"entity":"author"}}]}""")]
    [InlineData("""{"name":"broken","fields":[{"name":"parent","type":"guid","required":true,"references":{"entity":"author","onDelete":"setNull"}}]}""")]
    [InlineData("""{"name":"broken","fields":[{"name":"parent","type":"guid"}],"unique":[["missing"]]}""")]
    public async Task Invalid_relation_metadata_is_rejected(string definition)
    {
        factory.Define("broken", definition);

        var response = await client.GetAsync("/api/broken",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var body = await response
            .Content.
            ReadFromJsonAsync<JsonElement>(
                cancellationToken: TestContext.Current.CancellationToken);

        body.GetProperty("code").GetString().Should().Be("MTPR-3001");
    }

    [Fact]
    public async Task Rowid_field_does_not_change_insertion_order_or_paging_ties()
    {
        var ct = TestContext.Current.CancellationToken;
        factory.Define("item", """{"name":"item","fields":[{"name":"rowid","type":"string"},{"name":"label","type":"string"}]}""");
        var first = await create("item", new { rowid = "z", label = "same" });
        var second = await create("item", new { rowid = "a", label = "same" });

        var rows = await client.GetFromJsonAsync<JsonElement>("/api/item", cancellationToken: ct);
        rows[0].GetProperty("id").GetString().Should().Be(first);
        rows[1].GetProperty("id").GetString().Should().Be(second);

        var page = await client.GetFromJsonAsync<JsonElement>("/api/item?sort=label&skip=1&take=1", 
            cancellationToken: ct);

        page.GetArrayLength().Should().Be(1);
        page[0].GetProperty("id").GetString().Should().Be(second);
    }

    [Fact]
    public async Task Filters_sort_paging_and_selection_work_together()
    {
        var ct = TestContext.Current.CancellationToken;
        await create("book", new { title = "Alpha", price = 2 });
        await create("book", new { title = "Beta", price = 10 });
        await create("book", new { title = "Gamma", price = 20 });

        var rows = await client.GetFromJsonAsync<JsonElement>(
            "/api/book?filter=price:ge:2&sort=-price&skip=1&take=1&fields=title,price",
            cancellationToken: ct);

        rows.GetArrayLength().Should().Be(1);

        rows[0].GetProperty("title").GetString().Should().Be("Beta");

        rows[0].TryGetProperty("id", out _).Should().BeFalse();

        var contains = await client.GetFromJsonAsync<JsonElement>(
            "/api/book?filter=title:contains:ph",
            cancellationToken: ct);
        contains.GetArrayLength().Should().Be(1);

        (await client.GetAsync("/api/book?sort=unknown", ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);

        (await client.GetAsync("/api/book?filter=price:gt:not-a-number", ct))
            .StatusCode.Should()
            .Be(HttpStatusCode.BadRequest);

        (await client.GetAsync("/api/book?fields=title%2Cid%3Bdrop", ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Invalid_reference_definition_is_rejected()
    {
        factory.Define("broken",
            """{"name":"broken","fields":[{"name":"parent","type":"guid","references":{"entity":"missing"}}]}""");

        (await client.GetAsync("/api/broken",
            TestContext.Current.CancellationToken))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Decimal_filters_and_sort_preserve_decimal_precision()
    {
        var ct = TestContext.Current.CancellationToken;
        await create("book",
            new
            {
                title = "high",
                price = 1.0000000000000000000000000002m
            });
        await create("book",
            new
            {
                title = "low",
                price = 1.0000000000000000000000000001m
            });
        var rows = await client.GetFromJsonAsync<JsonElement>(
            "/api/book?filter=price:gt:1.0000000000000000000000000001&sort=price",
            cancellationToken: ct);

        rows.GetArrayLength().Should().Be(1);
        rows[0].GetProperty("title").GetString().Should().Be("high");
    }

    [Fact]
    public async Task Decimal_unique_fields_compare_numeric_values()
    {
        var ct = TestContext.Current.CancellationToken;
        factory.Define("amount", """{"name":"amount","fields":[{"name":"value","type":"decimal","required":true}],"unique":[["value"]]}""");
        await create("amount", new { value = 1.0m });

        (await client.PostAsJsonAsync("/api/amount",
            new { value = 1.00m },
            cancellationToken: ct))
            .StatusCode.Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Changing_an_existing_reference_is_rejected()
    {
        var ct = TestContext.Current.CancellationToken;
        (await client.GetAsync("/api/book", ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        factory.Define("book", """{"name":"book","fields":[{"name":"title","type":"string","required":true},{"name":"price","type":"decimal","required":true},{"name":"authorId","type":"guid","references":{"entity":"author","onDelete":"cascade"}}]}""");
        (await client.GetAsync("/api/book", ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Optional_reference_can_be_added_to_an_existing_entity()
    {
        var ct = TestContext.Current.CancellationToken;
        factory.Define("extra", """{"name":"extra","fields":[{"name":"title","type":"string"}]}""");
        var id = await create("extra", new { title = "preserved" });
        factory.Define("extra", """{"name":"extra","fields":[{"name":"title","type":"string"},{"name":"authorId","type":"guid","references":{"entity":"author","onDelete":"setNull"}}]}""");

        var row = await client.GetFromJsonAsync<JsonElement>($"/api/extra/{id}",
            cancellationToken: ct);

        row.GetProperty("title").GetString().Should().Be("preserved");
        row.GetProperty("authorId").ValueKind.Should().Be(JsonValueKind.Null);
        (await client.PatchAsJsonAsync($"/api/extra/{id}",
            new
            {
                authorId = Guid.NewGuid()
            },
            cancellationToken: ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }
}
