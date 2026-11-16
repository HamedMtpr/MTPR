using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Mtpr.Engine.Persistence;

public class SqliteConnectionFactory(string connectionString) : ISqliteConnectionFactory
{
    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(connectionString);
        connection.CreateCollation("MTPR_DECIMAL", (left, right) =>
            decimal.Parse(left, CultureInfo.InvariantCulture)
                .CompareTo(decimal.Parse(right, CultureInfo.InvariantCulture)));
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON";
        await command.ExecuteNonQueryAsync(cancellationToken);

        return connection;
    }
}
