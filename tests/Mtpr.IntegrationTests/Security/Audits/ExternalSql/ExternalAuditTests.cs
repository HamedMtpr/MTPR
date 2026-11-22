using System.Net;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security.Audits.ExternalSql;

public partial class ExternalAuditTests : IntegrationTest
{
    [Fact]
    public async Task Independent_connection_records_committed_changes_and_rolls_back_together()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var initialized =
            await client.GetAsync("/api/product", ct);
        initialized.StatusCode.Should().Be(HttpStatusCode.OK);

        await using SqliteConnection connection = new(
            $"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO product (id, title) VALUES ('a', 'first');
            UPDATE product SET title = 'second' WHERE id = 'a';
            DELETE FROM product WHERE id = 'a';
            """;
        await command.ExecuteNonQueryAsync(ct);

        await using (var transaction = connection.BeginTransaction())
        {
            command.Transaction = transaction;
            command.CommandText = 
                "INSERT INTO product (id, title) VALUES ('b', 'rollback')";
            await command.ExecuteNonQueryAsync(ct);
            await transaction.RollbackAsync(ct);
        }

        command.Transaction = null;
        command.CommandText = """
            SELECT operation
                , before_json
                , after_json
                , detail
                , subject
            FROM __mtpr_audit 
            ORDER BY sequence
            """;
        await using var reader = await command.ExecuteReaderAsync(ct);
        foreach (var (operation, before, after) in new[]
                 {
                     ("create", (string?)null, "first"),
                     ("update", "first", "second"),
                     ("delete", "second", (string?)null)
                 })
        {
            (await reader.ReadAsync(ct)).Should().BeTrue();
            reader.GetString(0).Should().Be(operation);
            value(reader, 1).Should().Be(before);
            value(reader, 2).Should().Be(after);
            reader.GetString(3).Should().Be("external-sql");
            reader.IsDBNull(4).Should().BeTrue();
        }
        (await reader.ReadAsync(ct)).Should().BeFalse();
    }

    [Fact]
    public async Task Replace_cannot_silently_delete_an_existing_row()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var initialized = 
            await client.GetAsync("/api/product", ct);
        initialized.StatusCode.Should().Be(HttpStatusCode.OK);

        await using SqliteConnection connection = new(
            $"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False");

        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO product (id, title) VALUES ('a', 'first')";
        await command.ExecuteNonQueryAsync(ct);
        command.CommandText = 
            "INSERT OR REPLACE INTO product (id, title) VALUES ('a', 'second')";

        await FluentActions.Invoking(async () => 
                await command.ExecuteNonQueryAsync(ct))
            .Should()
            .ThrowAsync<SqliteException>();

        command.CommandText = "SELECT title FROM product WHERE id = 'a'";
        (await command.ExecuteScalarAsync(ct)).Should().Be("first");
        command.CommandText = "SELECT COUNT(*) FROM __mtpr_audit";
        Convert.ToInt64(await command.ExecuteScalarAsync(ct)).Should().Be(1);
    }

    [Fact]
    public async Task Independent_connection_records_database_cascades()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        factory.Define("child", child);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var initialized = 
            await client.GetAsync("/api/product", ct);
        initialized.StatusCode.Should().Be(HttpStatusCode.OK);

        await using SqliteConnection connection = new(
            $"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys = ON;
            INSERT INTO product (id, title) VALUES ('a', 'first');
            INSERT INTO child (id, parentId) VALUES ('b', 'a');
            DELETE FROM product WHERE id = 'a';
            """;
        await command.ExecuteNonQueryAsync(ct);
        command.CommandText = """
            SELECT entity, operation FROM __mtpr_audit
            WHERE operation = 'delete' ORDER BY sequence
            """;
        await using var reader = await command.ExecuteReaderAsync(ct);
        (await reader.ReadAsync(ct)).Should().BeTrue();
        reader.GetString(0).Should().Be("child");
        (await reader.ReadAsync(ct)).Should().BeTrue();
        reader.GetString(0).Should().Be("product");
        (await reader.ReadAsync(ct)).Should().BeFalse();
    }

    [Fact]
    public async Task Audit_failure_rejects_the_external_change()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var initialized = 
            await client.GetAsync("/api/product", ct);
        initialized.StatusCode.Should().Be(HttpStatusCode.OK);

        await using SqliteConnection connection = new(
            $"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TRIGGER reject_external_audit BEFORE INSERT ON __mtpr_audit
            BEGIN
                SELECT RAISE(ABORT, 'audit unavailable');
            END;
            """;
        await command.ExecuteNonQueryAsync(ct);
        command.CommandText = "INSERT INTO product (id, title) VALUES ('a', 'first')";

        await FluentActions.Invoking(async () => 
                await command.ExecuteNonQueryAsync(ct))
            .Should().ThrowAsync<SqliteException>();

        command.CommandText = "SELECT COUNT(*) FROM product";
        Convert.ToInt64(await command.ExecuteScalarAsync(ct)).Should().Be(0);
    }

    [Theory]
    [InlineData("INSERT OR REPLACE INTO product (id, title) VALUES ('c', 'first')")]
    [InlineData("UPDATE OR REPLACE product SET title = 'first' WHERE id = 'b'")]
    public async Task Unique_conflicts_cannot_replace_another_record(string sql)
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", uniqueProduct);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var initialized = 
            await client.GetAsync("/api/product", ct);
        initialized.StatusCode.Should().Be(HttpStatusCode.OK);

        await using SqliteConnection connection = new(
            $"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO product (id, title) VALUES ('a', 'first');
            INSERT INTO product (id, title) VALUES ('b', 'second');
            """;
        await command.ExecuteNonQueryAsync(ct);
        command.CommandText = sql;
        await FluentActions.Invoking(async () => 
                await command.ExecuteNonQueryAsync(ct))
            .Should().ThrowAsync<SqliteException>();

        command.CommandText = "SELECT COUNT(*) FROM product";
        Convert.ToInt64(await command.ExecuteScalarAsync(ct)).Should().Be(2);
        command.CommandText = "SELECT COUNT(*) FROM __mtpr_audit";
        Convert.ToInt64(await command.ExecuteScalarAsync(ct)).Should().Be(2);
    }

    [Fact]
    public async Task Definition_reload_updates_external_audit_snapshots()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var initialized =
            await client.GetAsync("/api/product", ct);
        initialized.StatusCode.Should().Be(HttpStatusCode.OK);

        factory.Define("product", """
            {
              "name": "product",
              "fields": [
                { "name": "title", "type": "string" },
                { "name": "description", "type": "string" }
              ]
            }
            """);
        using var reloaded =
            await client.GetAsync("/api/product", ct);
        reloaded.StatusCode.Should().Be(HttpStatusCode.OK);

        await using SqliteConnection connection = new(
            $"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO product (id, title, description) VALUES ('a', 'first', 'new');
            """;
        await command.ExecuteNonQueryAsync(ct);
        command.CommandText = "SELECT after_json FROM __mtpr_audit";
        var snapshot = (string)(await command.ExecuteScalarAsync(ct))!;
        using var json = JsonDocument.Parse(snapshot);
        json.RootElement.GetProperty("description")
            .GetString()
            .Should()
            .Be("new");
    }

    private static string? value(SqliteDataReader reader, int index) =>
        reader.IsDBNull(index)
            ? null
            : JsonDocument.Parse(reader.GetString(index))
                .RootElement
                .GetProperty("title")
                .GetString();
}
