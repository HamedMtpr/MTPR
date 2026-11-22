namespace Mtpr.Engine.Definitions;

public interface IDefinitionCatalog
{
    /// <exception cref="Mtpr.Core.Exceptions.MtprException">The entity is not defined.</exception>
    Task<EntityDefinition> GetAsync(string entityName, CancellationToken cancellationToken);

    /// <summary>Returns every defined entity.</summary>
    Task<IReadOnlyList<EntityDefinition>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Prevents definition refresh until the returned lease is disposed.</summary>
    Task<IDisposable> AcquireReadLeaseAsync(CancellationToken cancellationToken);

    /// <summary>Pins the current catalog in the calling execution context while a lease is held.</summary>
    IDisposable PinSnapshot();
}
