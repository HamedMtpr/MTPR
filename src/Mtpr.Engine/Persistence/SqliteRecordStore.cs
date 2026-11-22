using Microsoft.Data.Sqlite;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Persistence.Audit;
using Mtpr.Engine.Persistence.AuditActorAccessors;

namespace Mtpr.Engine.Persistence;

public partial class SqliteRecordStore(ISqliteConnectionFactory connectionFactory,
    IAuditActorAccessor actor,
    IDefinitionCatalog catalog) : IRecordStore
{
    private const string idKey = EntityDefinitionVerifier.IdFieldName;

    public async Task<IReadOnlyDictionary<string, object?>> InsertAsync(EntityDefinition definition,
        IReadOnlyDictionary<string, object?> values,
        RecordRuleCallback rules,
        CancellationToken cancellationToken,
        IReadOnlyList<IReadOnlyList<string>>? scope = null)
    {
        var id = Guid.NewGuid();
        var columns = values.Keys.ToList();

        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(deferred: false);
        rules("beforeCreate", values, null);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var names = string.Join(", ", columns.Select(c => $"\"{c}\"").Prepend($"\"{idKey}\""));
        var markers = string.Join(", ", columns.Select((_, i) => $"$p{i}").Prepend("$id"));
        command.CommandText = $"INSERT INTO \"{definition.Name}\" ({names}) VALUES ({markers})";
        command.Parameters.AddWithValue("$id", id.ToString());
        bind(command, columns, values);
        await command.ExecuteNonQueryAsync(cancellationToken);

        var record = (await findAsync(connection, 
            transaction, 
            definition,
            id, 
            cancellationToken))!;

        rules("afterCreate", record, null);

        if (scope is not null && !await inScopeAsync(connection, transaction,
                definition, id, scope, cancellationToken))
            throw new Core.Exceptions.MtprException(
                Errors.MtprErrors.RecordNotFound,
                new Core.Exceptions.ExceptionParameter(definition.Name),
                new Core.Exceptions.ExceptionParameter(id));

        await auditAsync(connection, 
            transaction, 
            definition.Name, 
            id,
            "create",
            null, 
            record, 
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return record;
    }

    public async Task<IReadOnlyDictionary<string, object?>?> FindAsync(EntityDefinition definition,
        Guid id,
        CancellationToken cancellationToken,
        IReadOnlyList<IReadOnlyList<string>>? scope = null)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        if (scope is not null)
            return await findScopedAsync(connection,
                null, 
                definition, 
                id,
                scope, 
                cancellationToken);

        return await findAsync(connection, 
            null, 
            definition,
            id, 
            cancellationToken);
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ListAsync(EntityDefinition definition,
        RecordQuery query,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        var selected = SqliteQueryBuilder.Build(command, definition, query);
        command.Parameters.AddWithValue("$take", query.Take);
        command.Parameters.AddWithValue("$skip", query.Skip);
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = map(definition, reader);
            rows.Add(selected is null 
                ? row 
                : row.Where(p => selected.Contains(p.Key))
                    .ToDictionary(p => p.Key, p => p.Value));
        }
        return rows;
    }

    public async Task<IReadOnlyDictionary<string, object?>?> UpdateAsync(EntityDefinition definition,
        Guid id,
        IReadOnlyDictionary<string, object?> values,
        RecordComputeCallback compute,
        RecordRuleCallback rules,
        CancellationToken cancellationToken,
        IReadOnlyList<IReadOnlyList<string>>? scope = null)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(deferred: false);
        
        var old = 
            scope is null
                ? await findAsync(connection,
                    transaction, 
                    definition,
                    id, 
                    cancellationToken)
                : await findScopedAsync(connection, 
                    transaction, 
                    definition, 
                    id, 
                    scope, 
                    cancellationToken);
        
        if (old is null) 
            return null;

        var proposed = new Dictionary<string, object?>(old, 
            StringComparer.OrdinalIgnoreCase);

        foreach (var value in values) 
            proposed[value.Key] = value.Value;

        compute(proposed);

        rules("beforeUpdate", proposed, old);
        
        Dictionary<string, object?> writes =
            new(values, 
                StringComparer.OrdinalIgnoreCase);
        
        foreach (var field in definition.Fields
                     .Where(f => 
                         f.Computed.ValueKind != System.Text.Json.JsonValueKind.Undefined))
            writes[field.Name] = proposed[field.Name];

        var columns = writes.Keys.ToList();

        if (columns.Count > 0)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"UPDATE \"{definition.Name}\" SET " +
                string.Join(", ", columns.Select((c, i) => $"\"{c}\" = $p{i}")) + $" WHERE \"{idKey}\" = $id";
            command.Parameters.AddWithValue("$id", id.ToString());
            
            bind(command, 
                columns,
                writes);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        var record = (await findAsync(connection, 
            transaction, 
            definition, 
            id,
            cancellationToken))!;
        rules("afterUpdate", record, old);

        if (scope is not null && !await inScopeAsync(connection, 
                transaction,
                definition,
                id,
                scope, 
                cancellationToken))
            return null;

        await auditAsync(connection,
            transaction, 
            definition.Name,
            id, 
            "update", 
            old, 
            record,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return record;
    }

    public async Task<bool> DeleteAsync(EntityDefinition definition, Guid id,
        RecordRuleCallback rules, CancellationToken cancellationToken,
        IReadOnlyList<IReadOnlyList<string>>? scope = null)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(deferred: false);
        var old = scope is null
            ? await findAsync(connection, 
                transaction, 
                definition, 
                id, 
                cancellationToken)
            : await findScopedAsync(connection,
                transaction, 
                definition, 
                id,
                scope, 
                cancellationToken);

        if (old is null) 
            return false;

        rules("beforeDelete", old, old);

        // The database executes the cascades and set-nulls itself; this only
        // works out what it is going to touch so that every change is audited.
        var plan = await planCascadeAsync(catalog,
            connection,
            transaction,
            definition,
            id,
            cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"DELETE FROM \"{definition.Name}\" WHERE \"{idKey}\" = $id";
        command.Parameters.AddWithValue("$id", id.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);

        rules("afterDelete", old, old);

        await auditCascadeAsync(connection,
            transaction,
            definition.Name,
            id,
            plan,
            cancellationToken);

        await auditAsync(connection,
            transaction,
            definition.Name,
            id, 
            "delete",
            old, 
            null, 
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private Task auditAsync(SqliteConnection connection,
        SqliteTransaction transaction,
        string entity,
        Guid id,
        string operation,
        IReadOnlyDictionary<string, object?>? before,
        IReadOnlyDictionary<string, object?>? after,
        CancellationToken cancellationToken,
        string? causedBy = null) =>
        AuditTable.AppendAsync(connection,
            transaction,
            new AuditEntry(entity,
                id.ToString(),
                operation,
                before,
                after,
                CausedBy: causedBy),
            actor,
            cancellationToken);

    private static async Task<bool> inScopeAsync(SqliteConnection connection,
        SqliteTransaction transaction,
        EntityDefinition definition,
        Guid id,
        IReadOnlyList<IReadOnlyList<string>> scope, CancellationToken cancellationToken)
            => await findScopedAsync(connection, 
                transaction, 
                definition, 
                id, 
                scope, 
                cancellationToken) is not null;

    private static async Task<IReadOnlyDictionary<string, object?>?> findScopedAsync(
        SqliteConnection connection, SqliteTransaction? transaction, EntityDefinition definition,
        Guid id, IReadOnlyList<IReadOnlyList<string>> scope, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        SqliteQueryBuilder.Build(command, definition,
            new RecordQuery(0, 1, null, null, [$"id:eq:{id}"], scope));
        command.Parameters.AddWithValue("$take", 1);
        command.Parameters.AddWithValue("$skip", 0);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? map(definition, reader) : null;
    }

    private static async Task<IReadOnlyDictionary<string, object?>?> findAsync(SqliteConnection connection,
        SqliteTransaction? transaction,
        EntityDefinition definition,
        Guid id, 
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT * FROM \"{definition.Name}\" WHERE \"{idKey}\" = $id";
        command.Parameters.AddWithValue("$id", id.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) 
            ? map(definition, reader) 
            : null;
    }

    private static void bind(SqliteCommand command, 
        List<string> columns, 
        IReadOnlyDictionary<string, object?> values)
    {
        for (var i = 0; i < columns.Count; i++)
            command.Parameters.AddWithValue($"$p{i}", 
                SqliteTypeMap.ToDb(values[columns[i]]));
    }

    private static IReadOnlyDictionary<string, object?> map(EntityDefinition definition, 
        SqliteDataReader reader)
    {
        var row = new Dictionary<string, object?>
        {
            [idKey] =
                Guid.Parse(reader.GetString(reader.GetOrdinal(idKey)))
        };

        foreach (var field in definition.Fields)
            row[field.Name] = SqliteTypeMap.FromDb(field.Type, 
                reader.GetValue(reader.GetOrdinal(field.Name)));
        return row;
    }
}
