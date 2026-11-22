using System.Security.Claims;

namespace Mtpr.Api.Authorization;

/// <summary>Role grants of field names per entity, read from one configuration section.</summary>
public sealed class FieldGrants
{
    private readonly Dictionary<string, List<(string Role,
        HashSet<string> Fields)>> grants = 
            new(StringComparer.OrdinalIgnoreCase);

    public FieldGrants(IConfiguration configuration, string section)
    {
        this.Describe = section;

        foreach (var entity in configuration
                     .GetSection($"Mtpr:Authorization:{section}")
                     .GetChildren())
        {
            if (string.IsNullOrWhiteSpace(entity.Key) ||
                entity.Key == "*")
                throw new InvalidOperationException(
                    $"Field {Describe} grants require a named entity.");

            grants.Add(entity.Key, 
                parseRoles(entity));
        }
    }

    private string Describe =>
        field.Replace("Field", string.Empty)
            .ToLowerInvariant();

    /// <summary>Returns null when the entity has no policy, otherwise the caller's combined grants.</summary>
    public HashSet<string>? Permitted(ClaimsPrincipal user, string entity)
    {
        if (!grants.TryGetValue(entity, out var roles))
            return null;

        return roles
            .Where(grant => user.IsInRole(grant.Role))
            .SelectMany(grant => grant.Fields)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private List<(string Role, HashSet<string> Fields)> parseRoles(
        IConfigurationSection entity)
    {
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
                    $"Invalid field {Describe} grants for entity '{entity.Key}', role '{role.Key}'.");

            roles.Add((role.Key,
                new HashSet<string>(fields!,
                    StringComparer.OrdinalIgnoreCase)));
        }

        return roles;
    }
}