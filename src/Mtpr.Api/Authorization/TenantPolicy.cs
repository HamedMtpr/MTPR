using System.Security.Claims;
using System.Text.Json;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Definitions.Fields;

namespace Mtpr.Api.Authorization;

/// <summary>
/// Tenant isolation read from <c>Mtpr:Tenancy</c>. <c>ClaimType</c> names the claim
/// that carries the caller's tenant (default <c>tenant_id</c>) and
/// <c>Entities:{entity}</c> names the field that stores the tenant of a record.
/// For these entities every read and write is confined to the caller's tenant,
/// the tenant field is stamped on writes, and a caller without a tenant claim
/// is denied. Entities not listed (or set to null) are not tenant-scoped.
/// </summary>
public sealed class TenantPolicy
{
    private const string defaultClaimType = "tenant_id";

    private readonly string claimType;

    private readonly Dictionary<string, string> fields =
        new(StringComparer.OrdinalIgnoreCase);

    public TenantPolicy(IConfiguration configuration)
    {
        var section = configuration.GetSection("Mtpr:Tenancy");

        claimType = section["ClaimType"] ?? defaultClaimType;

        if (string.IsNullOrWhiteSpace(claimType))
            throw new InvalidOperationException(
                "Tenancy requires a nonempty claim type.");

        foreach (var entity in 
                 section.GetSection("Entities").GetChildren())
        {
            if (entity.Value is null)
                continue;

            if (string.IsNullOrWhiteSpace(entity.Key) ||
                entity.Key == "*" ||
                string.IsNullOrWhiteSpace(entity.Value))
                throw new InvalidOperationException(
                    "Tenancy requires a named entity and a tenant field.");

            fields.Add(entity.Key, entity.Value);
        }
    }

    public bool AppliesTo(string entity) => fields.ContainsKey(entity);

    public string? FieldOf(string entity) =>
        fields.GetValueOrDefault(entity);

    public IReadOnlyDictionary<string, string> Fields => fields;

    public bool IsValidDefinition(EntityDefinition definition)
    {
        if (!fields.TryGetValue(definition.Name, out var fieldName))
            return true;

        var field = definition.FindField(fieldName);

        return field is not null &&
               field.Type == FieldType.String &&
               field.Computed.ValueKind == JsonValueKind.Undefined &&
               field.References is null &&
               definition.Unique.All(group => group.Any(name =>
                   name.Equals(field.Name, StringComparison.OrdinalIgnoreCase)));
    }

    public bool HasTenantRelations(string entity,
        IReadOnlyList<EntityDefinition> definitions)
    {
        var connected = new HashSet<string>(fields.Keys,
            StringComparer.OrdinalIgnoreCase);

        var changed = true;

        while (changed)
        {
            changed = false;

            foreach (var definition in definitions)
            foreach (var field in definition.Fields
                         .Where(field => field.References is not null))
            {
                var target = field.References!.Entity;

                if (connected.Contains(definition.Name))
                    changed |= connected.Add(target);

                if (connected.Contains(target))
                    changed |= connected.Add(definition.Name);
            }
        }

        return connected.Contains(entity) && connected.Any(name => !fields.ContainsKey(name));
    }

    /// <summary>The caller's tenant, or null when the claim is missing or empty.</summary>
    public string? TenantOf(ClaimsPrincipal user)
    {
        var value = user.FindFirst(claimType)?.Value;

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }

    /// <summary>
    /// Narrows the row scope to the caller's tenant: every group gets the tenant filter,
    /// and without a row policy the tenant filter is the only group. Returns the
    /// original scope for entities that are not tenant-scoped.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>>? Scope(ClaimsPrincipal user,
        string entity,
        IReadOnlyList<IReadOnlyList<string>>? rowScope)
    {
        if (!fields.TryGetValue(entity, out var field))
            return rowScope;

        var tenant = TenantOf(user);

        if (tenant is null)
            return [];

        var filter = $"{field}:eq:{tenant}";

        return rowScope is null
            ? [[filter]]
            : rowScope
                .Select(group => (IReadOnlyList<string>)
                    [.. group, filter])
                .ToList();
    }

    /// <summary>
    /// True when the body names the tenant field with a value other than the caller's tenant.
    /// </summary>
    public bool Conflicts(ClaimsPrincipal user,
        string entity,
        JsonElement body)
    {
        if (!fields.TryGetValue(entity, out var field) ||
            body.ValueKind != JsonValueKind.Object)
            return false;

        var tenant = TenantOf(user);

        foreach (var property in body.EnumerateObject())
            if (string.Equals(property.Name,
                    field,
                    StringComparison.OrdinalIgnoreCase))
                return property.Value.ValueKind != JsonValueKind.String ||
                       property.Value.GetString() != tenant;

        return false;
    }

    /// <summary>Returns the input with the tenant field set to the caller's tenant.</summary>
    public IReadOnlyDictionary<string, JsonElement> Stamp(ClaimsPrincipal user,
        string entity,
        IReadOnlyDictionary<string, JsonElement> input)
    {
        var tenant = TenantOf(user);

        if (!fields.TryGetValue(entity, out var field) || tenant is null)
            return input;

        var stamped = new Dictionary<string, JsonElement>(input,
            StringComparer.OrdinalIgnoreCase)
        {
            [field] = JsonSerializer.SerializeToElement(tenant)
        };

        return stamped;
    }
}
