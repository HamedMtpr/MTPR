namespace Mtpr.Engine.Persistence.Tenancy;

public sealed record TenantWriteScope(string Tenant,
    IReadOnlyDictionary<string, string> Fields);

public interface ITenantWriteScopeAccessor
{
    TenantWriteScope? Current { get; }
}

internal sealed class EmptyTenantWriteScopeAccessor : ITenantWriteScopeAccessor
{
    public TenantWriteScope? Current => null;
}
