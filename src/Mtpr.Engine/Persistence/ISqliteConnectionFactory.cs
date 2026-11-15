using Microsoft.Data.Sqlite;

namespace Mtpr.Engine.Persistence;

public interface ISqliteConnectionFactory
{
    Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken);
}
