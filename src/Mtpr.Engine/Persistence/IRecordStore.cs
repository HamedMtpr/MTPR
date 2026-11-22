using Mtpr.Engine.Definitions;

namespace Mtpr.Engine.Persistence;

public delegate void RecordRuleCallback(string trigger, 
    IReadOnlyDictionary<string, object?> record,
    IReadOnlyDictionary<string, object?>? previous);

public delegate void RecordComputeCallback(IDictionary<string, object?> record);

public interface IRecordStore
{
    Task<IReadOnlyDictionary<string, object?>> InsertAsync(EntityDefinition definition,
        IReadOnlyDictionary<string, object?> values,
        RecordRuleCallback rules,
        CancellationToken cancellationToken,
        IReadOnlyList<IReadOnlyList<string>>? scope = null);

    Task<IReadOnlyDictionary<string, object?>?> FindAsync(EntityDefinition definition,
        Guid id,
        CancellationToken cancellationToken,
        IReadOnlyList<IReadOnlyList<string>>? scope = null);

    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ListAsync(EntityDefinition definition,
        RecordQuery query,
        CancellationToken cancellationToken);

    /// <returns>The updated record, or null when it does not exist.</returns>
    Task<IReadOnlyDictionary<string, object?>?> UpdateAsync(EntityDefinition definition,
        Guid id,
        IReadOnlyDictionary<string, object?> values,
        RecordComputeCallback compute,
        RecordRuleCallback rules,
        CancellationToken cancellationToken,
        IReadOnlyList<IReadOnlyList<string>>? scope = null);

    /// <returns>False when the record does not exist.</returns>
    Task<bool> DeleteAsync(EntityDefinition definition,
        Guid id,
        RecordRuleCallback rules,
        CancellationToken cancellationToken,
        IReadOnlyList<IReadOnlyList<string>>? scope = null);
}
