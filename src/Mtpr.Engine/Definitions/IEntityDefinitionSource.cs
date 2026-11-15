namespace Mtpr.Engine.Definitions;

public interface IEntityDefinitionSource
{
    /// <summary>Cheap token that changes whenever the underlying definitions change.</summary>
    string GetFingerprint();

    IReadOnlyList<EntityDefinition> Load();
}
