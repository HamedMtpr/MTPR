using Mtpr.Engine.Persistence.Tenancy;

namespace Mtpr.Api.Authorization;

public sealed class HttpTenantWriteScopeAccessor(IHttpContextAccessor accessor)
    : ITenantWriteScopeAccessor
{
    public TenantWriteScope? Current => accessor.HttpContext?.Items["Mtpr.TenantWriteScope"]
        as TenantWriteScope;
}
