using System.Text.Json;

namespace Mtpr.Engine;

public interface IEntityService
{
    Task<IReadOnlyDictionary<string, object?>> CreateAsync(string entity,
        IReadOnlyDictionary<string, JsonElement> input,
        CancellationToken cancellationToken,
        IReadOnlyList<IReadOnlyList<string>>? scope = null);

    Task<IReadOnlyDictionary<string, object?>> GetAsync(string entity,
        Guid id,
        CancellationToken cancellationToken,
        IReadOnlyList<IReadOnlyList<string>>? scope = null);

    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ListAsync(string entity,
        RecordQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, object?>> UpdateAsync(string entity,
        Guid id,
        IReadOnlyDictionary<string, JsonElement> input,
        bool partial,
        CancellationToken cancellationToken,
        IReadOnlyList<IReadOnlyList<string>>? scope = null);

    Task DeleteAsync(string entity,
        Guid id,
        CancellationToken cancellationToken,
        IReadOnlyList<IReadOnlyList<string>>? scope = null);
}
