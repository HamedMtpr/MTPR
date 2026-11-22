using System.Text.Json;
using Microsoft.Data.Sqlite;
using Mtpr.Engine.Persistence.AuditActorAccessors;

namespace Mtpr.Engine.Persistence.Audit;

/// <summary>Creates, upgrades, and appends to the audit table.</summary>
internal static class AuditTable
{
    private static readonly string[] upgradeColumns =
    [
        "subject_issuer",
        "request_id",
        "caused_by",
        "detail"
    ];

    /// <summary>
    /// Creates the table when missing, adds columns that older versions lack,
    /// and installs the triggers that make the table append-only.
    /// Runs inside the caller's transaction so a failure leaves nothing behind.
    /// </summary>
    public static async Task EnsureAsync(SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "__mtpr_audit" (
                "sequence" INTEGER PRIMARY KEY AUTOINCREMENT
                , "occurred_at" TEXT NOT NULL
                , "entity" TEXT NOT NULL
                , "record_id" TEXT NOT NULL
                , "operation" TEXT NOT NULL
                , "subject" TEXT
                , "subject_issuer" TEXT
                , "before_json" TEXT
                , "after_json" TEXT
                , "request_id" TEXT
                , "caused_by" TEXT
                , "detail" TEXT
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);

        command.CommandText = "PRAGMA table_info(\"__mtpr_audit\")";
        HashSet<string> existing = new(StringComparer.OrdinalIgnoreCase);

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                existing.Add(reader.GetString(1));
        }

        foreach (var column in upgradeColumns.Where(c => !existing.Contains(c)))
        {
            command.CommandText =
                $"ALTER TABLE \"__mtpr_audit\" ADD COLUMN \"{column}\" TEXT";

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        // Triggers come after the column upgrade: adding a column is not an UPDATE.
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "__mtpr_audit_store_write" (marker INTEGER);

            CREATE TRIGGER IF NOT EXISTS "__mtpr_audit_no_update"
            BEFORE UPDATE ON "__mtpr_audit"
            BEGIN
                SELECT RAISE(ABORT, 'The audit log is append-only.');
            END;

            CREATE TRIGGER IF NOT EXISTS "__mtpr_audit_no_delete"
            BEFORE DELETE ON "__mtpr_audit"
            BEGIN
                SELECT RAISE(ABORT, 'The audit log is append-only.');
            END;
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task AppendAsync(SqliteConnection connection,
        SqliteTransaction transaction,
        AuditEntry entry,
        IAuditActorAccessor actor,
        CancellationToken cancellationToken)
    {
        await EnsureAsync(connection, transaction, cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO "__mtpr_audit" ("occurred_at"
                , "entity"
                , "record_id"
                , "operation"
                , "subject"
                , "subject_issuer"
                , "before_json"
                , "after_json"
                , "request_id"
                , "caused_by"
                , "detail")
            VALUES ($time
                , $entity
                , $id
                , $operation
                , $subject
                , $issuer
                , $before
                , $after
                , $request
                , $causedBy
                , $detail);
            """;

        command.Parameters.AddWithValue("$time", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$entity", entry.Entity);
        command.Parameters.AddWithValue("$id", entry.RecordId);
        command.Parameters.AddWithValue("$operation", entry.Operation);
        command.Parameters.AddWithValue("$subject", (object?)actor.Subject ?? DBNull.Value);
        command.Parameters.AddWithValue("$issuer", (object?)actor.Issuer ?? DBNull.Value);
        command.Parameters.AddWithValue("$before", snapshot(entry.Before));
        command.Parameters.AddWithValue("$after", snapshot(entry.After));
        command.Parameters.AddWithValue("$request", (object?)actor.RequestId ?? DBNull.Value);
        command.Parameters.AddWithValue("$causedBy", (object?)entry.CausedBy ?? DBNull.Value);
        command.Parameters.AddWithValue("$detail", (object?)entry.Detail ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static object snapshot(IReadOnlyDictionary<string, object?>? values) =>
        values is null
            ? DBNull.Value
            : JsonSerializer.Serialize(values);
}
