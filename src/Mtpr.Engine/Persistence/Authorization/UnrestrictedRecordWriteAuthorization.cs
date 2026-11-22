using Mtpr.Engine.Definitions;

namespace Mtpr.Engine.Persistence.Authorization;

internal sealed class UnrestrictedRecordWriteAuthorization : IRecordWriteAuthorization
{
    public void EnsureCreate(EntityDefinition definition, IReadOnlyDictionary<string, object?> record) { }

    public void EnsureUpdate(EntityDefinition definition, IReadOnlyDictionary<string, object?> before,
        IReadOnlyDictionary<string, object?> after) { }

    public void EnsureDelete(EntityDefinition definition) { }
}