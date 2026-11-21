using Mtpr.Core.Exceptions;
using Mtpr.Engine.Errors;
using Mtpr.Engine.Persistence;

namespace Mtpr.Engine.Definitions;

/// <summary>Reloads and migrates definitions whenever the source fingerprint changes.</summary>
public class DefinitionCatalog(IEntityDefinitionSource source, 
    ISchemaMigrator migrator) : IDefinitionCatalog
{
    private readonly SemaphoreSlim gate = new(1, 1);

    private string? loadedFingerprint;

    private Dictionary<string, EntityDefinition> definitions =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task<EntityDefinition> GetAsync(string entityName,
        CancellationToken cancellationToken)
    {
        await refreshAsync(cancellationToken);

        return definitions.TryGetValue(entityName, out var definition)
            ? definition
            : throw new MtprException(MtprErrors.EntityNotFound,
                new ExceptionParameter(entityName));
    }

    public async Task<IReadOnlyList<EntityDefinition>> ListAsync(
        CancellationToken cancellationToken)
    {
        await refreshAsync(cancellationToken);

        return definitions.Values.ToList();
    }

    private async Task refreshAsync(CancellationToken cancellationToken)
    {
        if (source.GetFingerprint() == loadedFingerprint)
            return;

        await gate.WaitAsync(cancellationToken);

        try
        {
            var fingerprint = source.GetFingerprint();

            if (fingerprint == loadedFingerprint)
                return;

            var loaded = source.Load();
            await migrator.ApplyAsync(loaded, cancellationToken);

            definitions = loaded.ToDictionary(d => d.Name, 
                StringComparer.OrdinalIgnoreCase);
            loadedFingerprint = fingerprint;
        }
        finally
        {
            gate.Release();
        }
    }
}
