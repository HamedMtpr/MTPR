namespace Mtpr.Engine.Definitions;

public interface IEntityDefinitionVerifier
{
    /// <exception cref="Mtpr.Core.Exceptions.MtprException">The definition is invalid.</exception>
    void Verify(EntityDefinition definition, string origin);
}
