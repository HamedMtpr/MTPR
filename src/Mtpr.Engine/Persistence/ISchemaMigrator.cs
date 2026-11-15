using Mtpr.Engine.Definitions;

namespace Mtpr.Engine.Persistence;

public interface ISchemaMigrator
{
    /// <summary>Creates missing tables; rejects changes to already applied definitions.</summary>
    Task ApplyAsync(IReadOnlyList<EntityDefinition> definitions, 
        CancellationToken cancellationToken);
}
