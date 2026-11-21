namespace Mtpr.Engine.Definitions;

public interface IDefinitionCatalog
{
    /// <exception cref="Mtpr.Core.Exceptions.MtprException">The entity is not defined.</exception>
    Task<EntityDefinition> GetAsync(string entityName, CancellationToken cancellationToken);

    /// <summary>Returns every defined entity.</summary>
    Task<IReadOnlyList<EntityDefinition>> ListAsync(CancellationToken cancellationToken);
}
