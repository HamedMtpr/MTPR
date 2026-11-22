using System.Security.Claims;
using System.Text.Json;

namespace Mtpr.Api.Authorization;

/// <summary>Optional, deny-by-default input field grants for configured entities.</summary>
public sealed class FieldWritePermissions(IConfiguration configuration)
{
    private readonly FieldGrants grants = new(configuration, "FieldWrites");

    public bool AllowsFields(ClaimsPrincipal user,
        string entity,
        IEnumerable<string> fields)
    {
        var permitted = grants.Permitted(user, entity);

        return permitted is null || 
               permitted.Contains("*") ||
               fields.All(permitted.Contains);
    }

    public bool Allows(ClaimsPrincipal user,
        string entity,
        string method,
        JsonElement body)
    {
        var permitted = grants.Permitted(user, entity);

        if (permitted is null)
            return true;

        if (permitted.Count == 0)
            return false;

        if (method == "PUT")
            return permitted.Contains("*");

        if (body.ValueKind != JsonValueKind.Object)
            return false;

        return body.EnumerateObject()
            .All(property => permitted.Contains("*") ||
                             permitted.Contains(property.Name));
    }
}