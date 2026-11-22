using System.Security.Claims;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Persistence.Authorization;

namespace Mtpr.Api.Authorization;

public sealed record RecordWritePolicy(ClaimsPrincipal User,
    EntityPermissions Entities,
    FieldWritePermissions Fields);

public sealed class HttpRecordWriteAuthorization(IHttpContextAccessor accessor)
    : IRecordWriteAuthorization
{
    private RecordWritePolicy Policy =>
        accessor.HttpContext?.Items["Mtpr.RecordWritePolicy"] as RecordWritePolicy
        ?? throw new RecordWriteForbiddenException();

    public void EnsureCreate(EntityDefinition definition,
        IReadOnlyDictionary<string, object?> record)
    {
        var fields = definition.Fields
            .Where(field => record
                                .TryGetValue(field.Name, out var value) && 
                            value is not null)
            .Select(field => field.Name);

        ensure(Policy, definition.Name, "create", fields);
    }

    public void EnsureUpdate(EntityDefinition definition,
        IReadOnlyDictionary<string, object?> before,
        IReadOnlyDictionary<string, object?> after)
    {
        var fields = definition.Fields
            .Where(field => 
                !Equals(before[field.Name], after[field.Name]))
            .Select(field => field.Name);

        ensure(Policy, definition.Name, "update", fields);
    }

    public void EnsureDelete(EntityDefinition definition)
    {
        var policy = Policy;
        if (!policy.Entities.Allows(policy.User, 
                definition.Name, 
                "delete"))
            throw new RecordWriteForbiddenException();
    }

    private static void ensure(RecordWritePolicy policy, string entity,
        string action, IEnumerable<string> fields)
    {
        if (!policy.Entities.Allows(policy.User, entity, action) ||
            !policy.Fields.AllowsFields(policy.User, entity, fields))
            throw new RecordWriteForbiddenException();
    }
}