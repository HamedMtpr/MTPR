using Mtpr.Engine.Definitions;

namespace Mtpr.Engine.Persistence;

public interface IRecordStore
{
    Task<IReadOnlyDictionary<string, object?>> InsertAsync(EntityDefinition definition,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, object?>?> FindAsync(EntityDefinition definition,
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ListAsync(EntityDefinition definition,
        RecordQuery query,
        CancellationToken cancellationToken);

    /// <returns>False when the record does not exist.</returns>
    Task<bool> UpdateAsync(EntityDefinition definition,
        Guid id,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken);

    /// <returns>False when the record does not exist.</returns>
    Task<bool> DeleteAsync(EntityDefinition definition,
        Guid id,
        CancellationToken cancellationToken);
}
