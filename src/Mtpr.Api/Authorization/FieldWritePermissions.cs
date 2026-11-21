using System.Security.Claims;
using System.Text.Json;

namespace Mtpr.Api.Authorization;

/// <summary>Optional, deny-by-default input field grants for configured entities.</summary>
public sealed class FieldWritePermissions
{
    private readonly Dictionary<string, List<(string Role, HashSet<string> Fields)>> grants =
        new(StringComparer.OrdinalIgnoreCase);

    public FieldWritePermissions(IConfiguration configuration)
    {
        foreach (var entity in configuration
                     .GetSection("Mtpr:Authorization:FieldWrites")
                     .GetChildren())
        {
            if (string.IsNullOrWhiteSpace(entity.Key) ||
                entity.Key == "*")
                throw new InvalidOperationException("Field write grants require a named entity.");

            List<(string Role, HashSet<string> Fields)> roles = [];

            foreach (var role in entity.GetChildren())
            {
                var fields = role.GetChildren()
                    .Select(value => value.Value)
                    .ToArray();

                if (string.IsNullOrWhiteSpace(role.Key) ||
                    fields.Length == 0 ||
                    fields.Any(string.IsNullOrWhiteSpace))
                    throw new InvalidOperationException(
                        $"Invalid field write grants for entity '{entity.Key}', role '{role.Key}'.");

                roles.Add((role.Key,
                    new HashSet<string>(fields!, StringComparer.OrdinalIgnoreCase)));
            }

            grants.Add(entity.Key, roles);
        }
    }

    public bool Allows(ClaimsPrincipal user,
        string entity,
        string method,
        JsonElement body)
    {
        if (!grants.TryGetValue(entity, out var roles))
            return true;

        var permitted = roles
            .Where(grant => user.IsInRole(grant.Role))
            .SelectMany(grant => grant.Fields)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (permitted.Count == 0)
            return false;

        if (body.ValueKind != JsonValueKind.Object)
            return false;

        if (method == "PUT")
            return permitted.Contains("*");

        return body.EnumerateObject()
            .All(property => permitted.Contains("*") ||
                             permitted.Contains(property.Name));
    }
}
