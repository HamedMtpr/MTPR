using System.Security.Claims;

namespace Mtpr.Api.Authorization;

/// <summary>
/// Optional row-level filters per entity and role, read from
/// <c>Mtpr:Authorization:RowFilters:{entity}:{role}</c> as a list of
/// <c>field:operator:value</c> filters. A value of <c>{claim:type}</c> is replaced
/// by the caller's claim. Filters of one role are AND-ed, roles are OR-ed.
/// An entity with a policy exposes no rows to callers matching no role.
/// </summary>
public sealed class RowFilterPermissions
{
    private const string claimPrefix = "{claim:";

    private readonly Dictionary<string, List<(string Role,
        string[] Filters)>> policies =
            new(StringComparer.OrdinalIgnoreCase);

    public RowFilterPermissions(IConfiguration configuration)
    {
        foreach (var entity in configuration
                     .GetSection("Mtpr:Authorization:RowFilters")
                     .GetChildren())
        {
            if (string.IsNullOrWhiteSpace(entity.Key) ||
                entity.Key == "*")
                throw new InvalidOperationException(
                    "Row filters require a named entity.");

            policies.Add(entity.Key, parseRoles(entity));
        }
    }

    /// <summary>Returns null when the entity has no policy, otherwise the caller's filter groups.</summary>
    public IReadOnlyList<IReadOnlyList<string>>? ScopeOf(ClaimsPrincipal user,
        string entity)
    {
        if (!policies.TryGetValue(entity, out var roles))
            return null;

        List<IReadOnlyList<string>> groups = [];

        foreach (var (_, filters) in roles.Where(grant => user.IsInRole(grant.Role)))
        {
            var resolved = filters
                .Select(filter => resolve(user, filter))
                .ToList();

            if (resolved.All(filter => filter is not null))
                groups.Add(resolved!);
        }

        return groups;
    }

    private static string? resolve(ClaimsPrincipal user, string filter)
    {
        var parts = filter.Split(':', 3);

        if (!parts[2].StartsWith(claimPrefix) ||
            !parts[2].EndsWith('}'))
            return filter;

        var type = parts[2][claimPrefix.Length..^1];
        var value = user.FindFirst(type)?.Value;

        return string.IsNullOrEmpty(value)
            ? null
            : $"{parts[0]}:{parts[1]}:{value}";
    }

    private static List<(string Role, string[] Filters)> parseRoles(
        IConfigurationSection entity)
    {
        List<(string Role, string[] Filters)> roles = [];

        foreach (var role in entity.GetChildren())
        {
            var filters = role.GetChildren()
                .Select(value => value.Value ?? string.Empty)
                .ToArray();

            if (string.IsNullOrWhiteSpace(role.Key) ||
                filters.Length == 0 ||
                filters.Any(filter => !validFilter(filter)))
                throw new InvalidOperationException(
                    $"Invalid row filters for entity '{entity.Key}', role '{role.Key}'.");

            roles.Add((role.Key, filters));
        }

        return roles;
    }

    private static bool validFilter(string filter)
    {
        var parts = filter.Split(':', 3);

        if (parts.Length != 3 || string.IsNullOrWhiteSpace(parts[0]))
            return false;

        var operation = parts[1].ToLowerInvariant();

        if (operation is not ("eq" or "ne" or "gt" or "ge" or "lt" or "le"
            or "contains" or "isnull" or "notnull"))
            return false;

        if (operation is "isnull" or "notnull")
            return parts[2].Length == 0;

        if (parts[2].StartsWith(claimPrefix, StringComparison.Ordinal))
            return parts[2].EndsWith('}') && parts[2].Length > claimPrefix.Length + 1;

        return true;
    }
}
