using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Mtpr.Api.Authorization;

/// <summary>
/// Optional, deny-by-default output field grants for configured entities.
/// The record identifier is always readable.
/// </summary>
public sealed class FieldReadPermissions(IConfiguration configuration)
{
    private const string idField = "id";

    private readonly FieldGrants grants = new(configuration, "FieldReads");

    /// <summary>Checks that fields, sort and filters of a list query only use granted fields.</summary>
    public bool AllowsQuery(ClaimsPrincipal user,
        string entity,
        IQueryCollection query)
    {
        var permitted = grants.Permitted(user, entity);

        if (permitted is null)
            return true;

        if (permitted.Count == 0)
            return false;

        return referencedFields(query)
            .All(name => isReadable(permitted, name));
    }

    /// <summary>Removes ungranted fields from record results of a configured entity.</summary>
    public IResult Filter(ClaimsPrincipal user,
        string entity,
        IResult result)
    {
        var permitted = grants.Permitted(user, entity);

        if (permitted is null)
            return result;

        return result switch
        {
            Ok<IReadOnlyDictionary<string, object?>> ok =>
                Results.Ok(project(permitted, ok.Value!)),
            Created<IReadOnlyDictionary<string, object?>> created =>
                Results.Created(created.Location, project(permitted, created.Value!)),
            Ok<IReadOnlyList<IReadOnlyDictionary<string, object?>>> list =>
                Results.Ok(list.Value!
                    .Select(record => project(permitted, record))
                    .ToList()),
            IValueHttpResult => throw new InvalidOperationException(
                "A field-restricted entity returned a result that cannot be filtered."),
            _ => result
        };
    }

    private static IEnumerable<string> referencedFields(IQueryCollection query)
    {
        var selected = query["fields"]
            .SelectMany(value => (value ?? string.Empty).Split(','));

        var sorted = query["sort"]
            .SelectMany(value => (value ?? string.Empty).Split(','))
            .Select(item => item.TrimStart('-'));

        var filtered = query["filter"]
            .Select(value => (value ?? string.Empty).Split(':', 2)[0]);

        return selected
            .Concat(sorted)
            .Concat(filtered);
    }

    private static bool isReadable(HashSet<string> permitted, string name)
        => name.Equals(idField, StringComparison.OrdinalIgnoreCase) ||
           permitted.Contains("*") ||
           permitted.Contains(name);

    private static IReadOnlyDictionary<string, object?> project(
        HashSet<string> permitted,
        IReadOnlyDictionary<string, object?> record)
        => record
            .Where(pair => isReadable(permitted, pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
}