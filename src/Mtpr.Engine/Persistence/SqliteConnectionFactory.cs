using Microsoft.Data.Sqlite;

namespace Mtpr.Engine.Persistence;

public class SqliteConnectionFactory(string connectionString) : ISqliteConnectionFactory
{
    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return connection;
    }
}
