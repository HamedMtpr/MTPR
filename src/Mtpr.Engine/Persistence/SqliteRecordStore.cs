using Microsoft.Data.Sqlite;
using Mtpr.Engine.Definitions;

namespace Mtpr.Engine.Persistence;

public class SqliteRecordStore(ISqliteConnectionFactory connectionFactory) : IRecordStore
{
    private const string idKey = EntityDefinitionVerifier.IdFieldName;

    public async Task<IReadOnlyDictionary<string, object?>> InsertAsync(EntityDefinition definition,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var columns = values.Keys.ToList();

        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        var names = string.Join(", ", columns.Select(c => $"\"{c}\"").Prepend($"\"{idKey}\""));
        var markers = string.Join(", ", columns.Select((_, i) => $"$p{i}").Prepend("$id"));
        command.CommandText = $"INSERT INTO \"{definition.Name}\" ({names}) VALUES ({markers})";
        command.Parameters.AddWithValue("$id", id.ToString());
        bind(command, columns, values);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return (await FindAsync(definition, id, cancellationToken))!;
    }

    public async Task<IReadOnlyDictionary<string, object?>?> FindAsync(EntityDefinition definition,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM \"{definition.Name}\" WHERE \"{idKey}\" = $id";
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) 
            ? map(definition, reader) 
            : null;
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ListAsync(EntityDefinition definition,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM \"{definition.Name}\" ORDER BY rowid LIMIT $take OFFSET $skip";
        command.Parameters.AddWithValue("$take", take);
        command.Parameters.AddWithValue("$skip", skip);

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            rows.Add(map(definition, reader));

        return rows;
    }

    public async Task<bool> UpdateAsync(EntityDefinition definition,
        Guid id,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken)
    {
        var columns = values.Keys.ToList();

        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        if (columns.Count == 0)
            command.CommandText = $"SELECT COUNT(*) FROM \"{definition.Name}\" WHERE \"{idKey}\" = $id";
        else
            command.CommandText = $"UPDATE \"{definition.Name}\" SET " +
                                  string.Join(", ", columns.Select((c, i) => $"\"{c}\" = $p{i}")) +
                                  $" WHERE \"{idKey}\" = $id";

        command.Parameters.AddWithValue("$id", id.ToString());
        bind(command, columns, values);

        if (columns.Count == 0)
            return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) > 0;

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(EntityDefinition definition,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM \"{definition.Name}\" WHERE \"{idKey}\" = $id";
        command.Parameters.AddWithValue("$id", id.ToString());

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
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
