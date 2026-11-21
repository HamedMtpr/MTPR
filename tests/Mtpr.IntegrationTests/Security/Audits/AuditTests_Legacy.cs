using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security.Audits;

public partial class AuditTests : IntegrationTest
{
    [Fact]
    public async Task Failed_audit_insert_rolls_back_legacy_upgrade_and_record()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        var ct = TestContext.Current.CancellationToken;

        await using SqliteConnection connection =
            new($"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False");

        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE __mtpr_audit (
                sequence INTEGER PRIMARY KEY AUTOINCREMENT,
                occurred_at TEXT NOT NULL,
                entity TEXT NOT NULL,
                record_id TEXT NOT NULL,
                operation TEXT NOT NULL,
                subject TEXT,
                before_json TEXT,
                after_json TEXT
            );
            CREATE TRIGGER reject_audit
            BEFORE INSERT ON __mtpr_audit
            BEGIN
                SELECT RAISE(ABORT, 'audit unavailable');
            END;
            """;

        await command.ExecuteNonQueryAsync(ct);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/product",
            new { title = "first" },
            ct);

        response.IsSuccessStatusCode.Should().BeFalse();

        command.CommandText = """
            SELECT COUNT(*)
            FROM pragma_table_info('__mtpr_audit')
            WHERE name = 'subject_issuer'
            """;

        Convert.ToInt64(await command.ExecuteScalarAsync(ct))
            .Should()
            .Be(0);

        command.CommandText = "SELECT COUNT(*) FROM product";
        Convert.ToInt64(await command.ExecuteScalarAsync(ct))
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task Existing_audit_table_is_upgraded_without_losing_records()
    {
        await using MtprApiFactory factory = new();
        factory.Define("product", definition);
        var ct = TestContext.Current.CancellationToken;

        await using SqliteConnection connection =
            new($"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False");

        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE __mtpr_audit (
                sequence INTEGER PRIMARY KEY AUTOINCREMENT,
                occurred_at TEXT NOT NULL,
                entity TEXT NOT NULL,
                record_id TEXT NOT NULL,
                operation TEXT NOT NULL,
                subject TEXT,
                before_json TEXT,
                after_json TEXT
            );
            INSERT INTO __mtpr_audit
                (occurred_at
                , entity
                , record_id
                , operation
                , subject)
            VALUES ('2026-01-01T00:00:00Z'
            , 'product'
            , 'old'
            , 'create'
            , 'old-user');
            """;

        await command.ExecuteNonQueryAsync(ct);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/product",
            new { title = "first" },
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        command.CommandText = """
            SELECT subject, subject_issuer
            FROM __mtpr_audit
            ORDER BY sequence
            """;

        await using var reader = await command.ExecuteReaderAsync(ct);
        (await reader.ReadAsync(ct)).Should().BeTrue();
        reader.GetString(0).Should().Be("old-user");
        reader.IsDBNull(1).Should().BeTrue();
        (await reader.ReadAsync(ct)).Should().BeTrue();
        reader.GetString(0).Should().Be("test-user");
        reader.IsDBNull(1).Should().BeTrue();
        (await reader.ReadAsync(ct)).Should().BeFalse();
    }
}