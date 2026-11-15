using System.Text.Json;

namespace Mtpr.Engine;

public interface IEntityService
{
    Task<IReadOnlyDictionary<string, object?>> CreateAsync(string entity,
        IReadOnlyDictionary<string, JsonElement> input,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, object?>> GetAsync(string entity, 
        Guid id, 
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ListAsync(string entity,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, object?>> UpdateAsync(string entity,
        Guid id,
        IReadOnlyDictionary<string, JsonElement> input,
        bool partial,
        CancellationToken cancellationToken);

    Task DeleteAsync(string entity, 
        Guid id, 
        CancellationToken cancellationToken);
}
