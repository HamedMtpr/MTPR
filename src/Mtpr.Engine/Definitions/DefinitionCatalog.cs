using Mtpr.Core.Exceptions;
using Mtpr.Engine.Errors;
using Mtpr.Engine.Persistence;

namespace Mtpr.Engine.Definitions;

/// <summary>Reloads and migrates definitions whenever the source fingerprint changes.</summary>
public class DefinitionCatalog(IEntityDefinitionSource source, 
    ISchemaMigrator migrator) : IDefinitionCatalog
{
    private sealed class SnapshotLease(
        AsyncLocal<Dictionary<string, EntityDefinition>?> pinned,
        Dictionary<string, EntityDefinition>? previous) : IDisposable
    {
        public void Dispose() => pinned.Value = previous;
    }

    private sealed class ReadLease(SemaphoreSlim gate) : IDisposable
    {
        private SemaphoreSlim? held = gate;

        public void Dispose() => Interlocked.Exchange(ref held, null)?.Release();
    }

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly AsyncLocal<Dictionary<string, EntityDefinition>?> pinned = new();

    private string? loadedFingerprint;

    private Dictionary<string, EntityDefinition> definitions =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task<EntityDefinition> GetAsync(string entityName,
        CancellationToken cancellationToken)
    {
        if (pinned.Value is { } snapshot)
            return snapshot.TryGetValue(entityName, out var item)
                ? item
                : throw new MtprException(MtprErrors.EntityNotFound,
                    new ExceptionParameter(entityName));

        await refreshAsync(cancellationToken);

        return definitions.TryGetValue(entityName, out var definition)
            ? definition
            : throw new MtprException(MtprErrors.EntityNotFound,
                new ExceptionParameter(entityName));
    }

    public async Task<IReadOnlyList<EntityDefinition>> ListAsync(
        CancellationToken cancellationToken)
    {
        if (pinned.Value is { } snapshot)
            return snapshot.Values.ToList();

        await refreshAsync(cancellationToken);

        return definitions.Values.ToList();
    }

    public IDisposable PinSnapshot()
    {
        var previous = pinned.Value;
        pinned.Value = definitions;
        return new SnapshotLease(pinned, previous);
    }

    public async Task<IDisposable> AcquireReadLeaseAsync(CancellationToken cancellationToken)
    {
        await refreshAsync(cancellationToken);
        await gate.WaitAsync(cancellationToken);
        return new ReadLease(gate);
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
