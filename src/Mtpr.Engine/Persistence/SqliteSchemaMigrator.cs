using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Mtpr.Core.Exceptions;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Errors;

namespace Mtpr.Engine.Persistence;

public class SqliteSchemaMigrator(ISqliteConnectionFactory connectionFactory) : ISchemaMigrator
{
    private const string logTable = "__mtpr_definitions";

    public async Task ApplyAsync(IReadOnlyList<EntityDefinition> definitions,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var transaction = 
            (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await execute(connection, transaction,
            $"CREATE TABLE IF NOT EXISTS {logTable} (name TEXT PRIMARY KEY, hash TEXT NOT NULL, applied_at TEXT NOT NULL)",
            cancellationToken);

        foreach (var definition in definitions)
            await applyAsync(connection, transaction, definition, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task applyAsync(SqliteConnection connection,
        SqliteTransaction transaction,
        EntityDefinition definition,
        CancellationToken cancellationToken)
    {
        var hash = computeHash(definition);
        var applied = await appliedHashAsync(connection, 
            transaction, 
            definition.Name, 
            cancellationToken);

        if (applied == hash)
            return;

        if (applied is not null)
            throw new MtprException(MtprErrors.SchemaChangeNotSupported,
                new ExceptionParameter(definition.Name),
                new ExceptionParameter("changing an already applied definition is not supported yet."));

        await execute(connection, transaction, createTableSql(definition), cancellationToken);

        await using var log = connection.CreateCommand();
        log.Transaction = transaction;
        log.CommandText = $"INSERT INTO {logTable} (name, hash, applied_at) VALUES ($n, $h, $t)";
        log.Parameters.AddWithValue("$n", definition.Name);
        log.Parameters.AddWithValue("$h", hash);
        log.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("O"));
        await log.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<string?> appliedHashAsync(SqliteConnection connection,
        SqliteTransaction transaction,
        string name,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT hash FROM {logTable} WHERE name = $n";
        command.Parameters.AddWithValue("$n", name);

        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }

    private static string createTableSql(EntityDefinition definition)
    {
        var columns = definition.Fields.Select(f =>
            $"\"{f.Name}\" {SqliteTypeMap.ColumnType(f.Type)}{(f.Required ? " NOT NULL" : string.Empty)}");

        return $"CREATE TABLE \"{definition.Name}\" (\"{EntityDefinitionVerifier.IdFieldName}\" TEXT PRIMARY KEY, " +
               $"{string.Join(", ", columns)})";
    }

    private static string computeHash(EntityDefinition definition)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            Name = definition.Name.ToLowerInvariant(),
            Fields = definition.Fields.Select(f => new
            {
                f.Name, Type = f.Type.ToString(), 
                f.Required, f.MinLength, 
                f.MaxLength,
                f.Min, 
                f.Max
            })
        });

        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(canonical)));
    }

    private static async Task execute(SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
