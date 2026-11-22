namespace Mtpr.Engine.Persistence.AuditActorAccessors;

internal sealed class EmptyAuditActorAccessor : IAuditActorAccessor
{
    public string? Subject => null;
}