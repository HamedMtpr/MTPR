using System.Security.Claims;
using Mtpr.Engine.Persistence.AuditActorAccessors;

namespace Mtpr.Api;

public sealed class HttpAuditActorAccessor(IHttpContextAccessor context) 
    : IAuditActorAccessor
{
    public string? Subject => 
        context.HttpContext
            ?.User
            .FindFirst(ClaimTypes.NameIdentifier)
            ?.Value;

    public string? Issuer =>
        context.HttpContext
            ?.User
            .FindFirst("iss")
            ?.Value;
}
