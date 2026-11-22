using Microsoft.Data.Sqlite;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Definitions.Fields;

namespace Mtpr.Engine.Persistence.Audit;

/// <summary>Installs transactional audit triggers for writes made outside the record store.</summary>
internal static class SqliteExternalAudit
{
    extension(SqliteConnection connection)
    {
        public async Task InstallAsync(SqliteTransaction transaction,
            EntityDefinition definition,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;

            foreach (var action in new[] { "INSERT", "UPDATE" })
            {
                var trigger = $"__mtpr_external_{definition.Name}_guard_{action}";
                var unique = definition.Unique.Select(group =>
                    "(" + string.Join(" AND ", group.Select(name =>
                        $"existing.\"{name}\" = NEW.\"{name}\"" +
                        (definition.FindField(name)!.Type == FieldType.Decimal
                            ? " COLLATE MTPR_DECIMAL"
                            : string.Empty))) + ")");
                var conflicts = new[]
                {
                    "existing.\"id\" = NEW.\"id\""
                }.Concat(unique);

                var otherRow = action == "UPDATE" 
                    ? "existing.rowid <> OLD.rowid AND " 
                    : string.Empty;

                command.CommandText = $"""
                                       DROP TRIGGER IF EXISTS "{trigger}";
                                       CREATE TRIGGER "{trigger}"
                                       BEFORE {action} ON "{definition.Name}"
                                       WHEN EXISTS (SELECT 1 FROM "{definition.Name}" AS existing
                                           WHERE {otherRow}({string.Join(" OR ", conflicts)}))
                                       BEGIN
                                           SELECT RAISE(ABORT, 'Conflicting writes cannot replace existing rows.');
                                       END;
                                       """;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var (operation, sqlAction, before, after, id) in new[]
                     {
                         ("create", "INSERT", "NULL", snapshot(definition, "NEW"), "NEW"),
                         ("update", "UPDATE", snapshot(definition, "OLD"), snapshot(definition, "NEW"), "NEW"),
                         ("delete", "DELETE", snapshot(definition, "OLD"), "NULL", "OLD")
                     })
            {
                var trigger = $"__mtpr_external_{definition.Name}_{operation}";
                command.CommandText = $"""
                                       DROP TRIGGER IF EXISTS "{trigger}";
                                       CREATE TRIGGER "{trigger}"
                                       AFTER {sqlAction} ON "{definition.Name}"
                                       WHEN NOT EXISTS (SELECT 1 FROM "__mtpr_audit_store_write")
                                       BEGIN
                                           INSERT INTO "__mtpr_audit"
                                               ("occurred_at", "entity", "record_id", "operation",
                                                "before_json", "after_json", "detail")
                                           VALUES (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'),
                                               '{definition.Name}', {id}."id", '{operation}',
                                               {before}, {after}, 'external-sql');
                                       END;
                                       """;

                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public async Task SetStoreWriteAsync(SqliteTransaction transaction,
            bool active,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = active
                ? "INSERT INTO \"__mtpr_audit_store_write\" DEFAULT VALUES"
                : "DELETE FROM \"__mtpr_audit_store_write\"";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static string snapshot(EntityDefinition definition, string row)
    {
        var fields = definition.Fields.Select(field => field.Name).Prepend("id");
        var values =
            fields.Select(name => $"SELECT '{name}' AS name, {row}.\"{name}\" AS value");

        return 
            $"(SELECT json_group_object(name, value) FROM ({string.Join(" UNION ALL ", values)}))";
    }
}
