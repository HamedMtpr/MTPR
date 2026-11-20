using System.Security.Claims;

namespace Mtpr.Api;

/// <summary>Immutable, deny-by-default API permissions loaded at startup.</summary>
public sealed class EntityPermissions
{
    private static readonly HashSet<string> Operations = new(StringComparer.Ordinal)
    {
        "create", 
        "read", 
        "update",
        "delete"
    };
    private readonly List<(string Role,
        string Entity, 
        HashSet<string> Actions)> grants = [];

    public EntityPermissions(IConfiguration configuration)
    {
        foreach (var role in 
                 configuration.GetSection("Mtpr:Authorization:Roles")
                     .GetChildren())
            foreach (var entity in role.GetChildren())
            {
                var actions = entity.GetChildren()
                    .Select(value => value.Value)
                    .ToArray();

                if (string.IsNullOrWhiteSpace(role.Key) || 
                    string.IsNullOrWhiteSpace(entity.Key) ||
                    actions.Length == 0 || 
                    actions.Any(action => action is null ||
                                          !Operations.Contains(action)))
                    throw new InvalidOperationException(
                        $"Invalid permissions for role '{role.Key}', entity '{entity.Key}'.");

                grants.Add((role.Key, 
                    entity.Key, 
                    new HashSet<string>(actions!, 
                        StringComparer.Ordinal)));
            }
    }

    public bool Allows(ClaimsPrincipal user, string entity, string action)
        => grants.Any(grant => user.IsInRole(grant.Role) &&
                               (grant.Entity == "*" || 
                                string.Equals(grant.Entity, 
                                    entity,
                                    StringComparison.OrdinalIgnoreCase)) && 
                               grant.Actions.Contains(action));
}
