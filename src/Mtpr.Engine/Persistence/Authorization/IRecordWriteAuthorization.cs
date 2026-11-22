using Mtpr.Engine.Definitions;

namespace Mtpr.Engine.Persistence.Authorization;

public interface IRecordWriteAuthorization
{
    void EnsureCreate(EntityDefinition definition, IReadOnlyDictionary<string, object?> record);
    void EnsureUpdate(EntityDefinition definition, IReadOnlyDictionary<string, object?> before,
        IReadOnlyDictionary<string, object?> after);
    void EnsureDelete(EntityDefinition definition);
}