using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.ComputedValue;

public partial class ComputedValueTests : IntegrationTest, IDisposable
{
    [Fact]
    public async Task Create_and_patch_compute_values_before_rules_and_reject_client_writes()
    {
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var created = 
            await client.PostAsJsonAsync("/api/line", 
                new
                {
                    price = 10m
                }, 
                ct);

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var row = 
            await created.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        row.GetProperty("total").GetDecimal().Should().Be(10);

        var id = row
            .GetProperty("id")
            .GetString();

        var patch = 
            await client.PatchAsJsonAsync($"/api/line/{id}",
                new
                {
                    quantity = 3
                },
                ct);

        patch.StatusCode.Should().Be(HttpStatusCode.OK);

        (await patch.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("total")
            .GetDecimal()
            .Should()
            .Be(30);

        (await client.PatchAsJsonAsync($"/api/line/{id}",
            new
            {
                quantity = 6
            },
            ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);

        var persisted = 
            await client.GetFromJsonAsync<JsonElement>($"/api/line/{id}",
                ct);

        persisted.GetProperty("total")
            .GetDecimal()
            .Should()
            .Be(30);

        var readOnly =
            await client.PatchAsJsonAsync($"/api/line/{id}", 
                new
                {
                    TOTAL = 7
                }, 
                ct);

        readOnly.StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);

        var error = 
            await readOnly.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        error.GetProperty("errors")[0]
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("MTPR-2013");

        (await client.PostAsJsonAsync("/api/line",
            new
            {
                price = 10, total = 10
            },
            ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);

        (await client.PostAsJsonAsync("/api/line",
            new
            {
                price = 200
            },
            ct))
            .StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Changing_expression_affects_next_write_without_rebuilding()
    {
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var created = 
            await client.PostAsJsonAsync("/api/line",
                new
                {
                    price = 10m,
                    quantity = 2
                }, 
                ct);

        var id = 
            (await created.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("id")
            .GetString();

        factory.Define("line", 
            definition.Replace("\"operator\": \"multiply\"", "\"operator\": \"add\""));

        var old = await client.GetFromJsonAsync<JsonElement>($"/api/line/{id}", ct);
        old.GetProperty("total")
            .GetDecimal()
            .Should()
            .Be(20);

        var patch = 
            await client.PatchAsJsonAsync($"/api/line/{id}", 
                new
                {
                    quantity = 3
                }, 
                ct);
        patch.StatusCode.Should().Be(HttpStatusCode.OK);

        (await patch.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("total")
            .GetDecimal()
            .Should()
            .Be(13);
    }

    [Theory]
    [InlineData("{\"field\":\"missing\"}")]
    [InlineData("{\"field\":\"total\"}")]
    [InlineData("{\"operator\":\"divide\",\"left\":{\"value\":1},\"right\":{\"value\":0}}")]
    public async Task Invalid_expressions_reject_definition(string expression)
    {
        factory.Define("line", 
            $$"""
              { 
                 "name": "line",
                  "fields": [ 
                  { 
                     "name": "price", 
                     "type": "decimal" 
                  },
                  { 
                  "name": "total", 
                  "type": "decimal", 
                  "computed": {{expression}} } ] 
              }
          """);

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var response = await client.PostAsJsonAsync("/api/line",
            new { },
            ct);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.InternalServerError);

        (await response.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("MTPR-3001");
    }

    [Fact]
    public async Task Concurrent_patches_recompute_from_the_committed_record()
    {
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var created = 
            await client.PostAsJsonAsync("/api/line",
                new
                {
                    price = 1m
                }, 
                ct);

        var id = 
            (await created.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("id")
            .GetString();

        var responses = 
            await Task.WhenAll(
            client.PatchAsJsonAsync($"/api/line/{id}", 
                new
                {
                    price = 2m
                }, 
                ct), 
            client.PatchAsJsonAsync($"/api/line/{id}",
                new
                {
                    quantity = 3
                }, 
                ct));

        responses.Should()
            .OnlyContain(response => 
                response.StatusCode == HttpStatusCode.OK);

        var row = await client.GetFromJsonAsync<JsonElement>(
            $"/api/line/{id}", 
            ct);

        row.GetProperty("total").GetDecimal().Should().Be(6);

        var replaced = 
            await client.PutAsJsonAsync($"/api/line/{id}", 
                new
                {
                    price = 4m, 
                    quantity = 2
                }, 
                ct);
        replaced.StatusCode.Should().Be(HttpStatusCode.OK);

        (await replaced.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("total")
            .GetDecimal()
            .Should()
            .Be(8);
    }

    [Fact]
    public async Task Decimal_overflow_is_a_validation_error()
    {
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var response = await client.PostAsJsonAsync("/api/line",
            new
            {
                price = decimal.MaxValue,
                quantity = 2
            }, 
            ct);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);

        var body = 
            await response.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        body.GetProperty("errors")[0]
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("MTPR-2014");
    }

    [Fact]
    public async Task Missing_optional_operand_produces_null_for_optional_computed_field()
    {
        factory.Define("line", 
            """
            {
               "name": "line", 
               "fields": [
               { 
                  "name": "price", 
                  "type": "decimal" 
               },
               { 
                  "name": "total", 
                  "type": "decimal", 
                  "computed": 
                  { 
                     "operator": "add", 
                     "left": { "field": "price" }, 
                     "right": { "value": 1 } 
                  } 
               }] 
            }
            """);

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var response = await client.PostAsJsonAsync("/api/line", 
            new { },
            ct);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var row = await response.Content
            .ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        row.GetProperty("total")
            .ValueKind
            .Should()
            .Be(JsonValueKind.Null);
    }
}

