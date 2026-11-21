using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security.Audits;

public partial class AuditTests : IntegrationTest
{
    [Fact]
    public async Task Successful_writes_record_actor_and_before_after_snapshots()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var created =
            await client.PostAsJsonAsync("/api/product",
                new
                {
                    title = "first"
                },
                ct);

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var id =
            (await created.Content
                .ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString();

        using var updated =
            await client.PatchAsJsonAsync($"/api/product/{id}",
                new
                {
                    title = "second"
                },
                ct);

        updated.StatusCode.Should().Be(HttpStatusCode.OK);

        using var read =
            await client.GetAsync($"/api/product/{id}", ct);

        read.StatusCode.Should().Be(HttpStatusCode.OK);

        using var deleted =
            await client.DeleteAsync($"/api/product/{id}", ct);

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using SqliteConnection connection =
            new($"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False");

        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT entity
                              , record_id
                              , operation
                              , subject
                              , before_json
                              , after_json
                              , occurred_at 
                              FROM __mtpr_audit 
                              ORDER BY sequence
                              """;
        await using var reader = await command.ExecuteReaderAsync(ct);
        foreach (var (operation, before, after) in 
                 new[]
                 {
                     ("create", null, "first"),
                     ("update", "first", "second"), 
                     ("delete", "second", null)
                 })
        {
            (await reader.ReadAsync(ct)).Should().BeTrue();
            reader.GetString(0).Should().Be("product");
            reader.GetString(1).Should().Be(id);
            reader.GetString(2).Should().Be(operation);
            reader.GetString(3).Should().Be("test-user");
            (reader.IsDBNull(4) 
                ? null 
                : JsonDocument.Parse(reader.GetString(4))
                    .RootElement
                    .GetProperty("title")
                    .GetString())
                .Should()
                .Be(before);

            (reader.IsDBNull(5) 
                ? null 
                : JsonDocument.Parse(reader.GetString(5))
                    .RootElement
                    .GetProperty("title")
                    .GetString())
                .Should()
                .Be(after);

            DateTimeOffset.Parse(reader.GetString(6)).
                Offset
                .Should()
                .Be(TimeSpan.Zero);
        }

        (await reader.ReadAsync(ct)).Should().BeFalse();
    }

    [Fact]
    public async Task Rejected_rule_and_audit_write_failure_leave_record_and_audit_unchanged()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var created =
            await client.PostAsJsonAsync("/api/product",
                new
                {
                    title = "first"
                }, 
                ct);

        var id = 
            (await created.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString();

        using var rejected = 
            await client.PatchAsJsonAsync($"/api/product/{id}",
                new
                {
                    title = "blocked"
                }, 
                ct);

        rejected.StatusCode
            .Should()
            .Be(HttpStatusCode.UnprocessableEntity);

        await using SqliteConnection connection =
            new($"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False");

        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
                              CREATE TRIGGER reject_audit 
                              BEFORE INSERT ON __mtpr_audit 
                              BEGIN 
                                SELECT RAISE(ABORT, 'audit unavailable'); 
                              END
                              """;
        await command.ExecuteNonQueryAsync(ct);

        using var failed = 
            await client.PatchAsJsonAsync($"/api/product/{id}", 
                new
                {
                    title = "second"
                }, 
                ct);

        failed.IsSuccessStatusCode.Should().BeFalse();

        using var failedCreate = 
            await client.PostAsJsonAsync("/api/product",
                new
                {
                    title = "new"
                },
                ct);

        failedCreate.IsSuccessStatusCode.Should().BeFalse();

        using var failedDelete =
            await client.DeleteAsync($"/api/product/{id}", ct);

        failedDelete.IsSuccessStatusCode.Should().BeFalse();

        using var fetched = 
            await client.GetAsync($"/api/product/{id}", ct);

        (await fetched.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("title")
            .GetString()
            .Should()
            .Be("first");

        using var list =
            await client.GetAsync("/api/product",
                ct);

        (await list.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetArrayLength()
            .Should()
            .Be(1);

        command.CommandText = "SELECT COUNT(*) FROM __mtpr_audit";
        Convert.ToInt64(
            await command.ExecuteScalarAsync(ct))
            .Should()
            .Be(1);
    }
}