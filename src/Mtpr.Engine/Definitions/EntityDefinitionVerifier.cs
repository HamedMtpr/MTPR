using System.Text.RegularExpressions;
using Mtpr.Core.Exceptions;
using Mtpr.Engine.Errors;

namespace Mtpr.Engine.Definitions;

public partial class EntityDefinitionVerifier : IEntityDefinitionVerifier
{
    public const string IdFieldName = "id";

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]*$")]
    private static partial Regex identifierRegex();

    public void Verify(EntityDefinition definition, string origin)
    {
        if (!isIdentifier(definition.Name))
            throw invalid(origin,
                $"entity name '{definition.Name}' is not a valid identifier");

        if (definition.Fields is null || definition.Fields.Count == 0)
            throw invalid(origin,
                "at least one field is required");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in definition.Fields)
        {
            if (field is null)
                throw invalid(origin,
                    "fields cannot contain null");

            verifyField(field, seen, origin);
        }

        if (definition.Unique is null)
            throw invalid(origin, "unique cannot be null");
        foreach (var group in definition.Unique)
        {
            if (group is null || group.Count == 0 ||
                group.Any(name => name is null || definition.FindField(name) is null) ||
                group.Distinct(StringComparer.OrdinalIgnoreCase).Count() != group.Count)
                throw invalid(origin, "unique must contain nonempty groups of distinct defined fields");
        }
    }

    private static void verifyField(FieldDefinition field, HashSet<string> seen, string origin)
    {
        if (!isIdentifier(field.Name))
            throw invalid(origin,
                $"field name '{field.Name}' is not a valid identifier");

        if (field.Name.Equals(IdFieldName, StringComparison.OrdinalIgnoreCase))
            throw invalid(origin,
                "field name 'id' is reserved");

        if (!seen.Add(field.Name))
            throw invalid(origin,
                $"duplicate field '{field.Name}'");

        if (!Enum.IsDefined(field.Type))
            throw invalid(origin,
                $"field '{field.Name}' has an unknown type");

        if (field.References is { } reference &&
            (field.Type != FieldType.Guid ||
             !isIdentifier(reference.Entity) ||
             !Enum.IsDefined(reference.OnDelete) ||
             (reference.OnDelete == DeleteBehavior.SetNull && field.Required)))
            throw invalid(origin, $"field '{field.Name}' has an invalid reference");

        if (field.MinLength < 0 || field.MaxLength < 0 || field.MinLength > field.MaxLength || field.Min > field.Max)
            throw invalid(origin,
                $"field '{field.Name}' has a minimum greater than its maximum");

        var isString = field.Type == FieldType.String;

        if (!isString && (field.MinLength is not null || field.MaxLength is not null))
            throw invalid(origin,
                $"field '{field.Name}': length constraints apply to string only");

        if (field.Type is not (FieldType.Int or FieldType.Long or
                FieldType.Decimal or FieldType.Double) &&
            (field.Min is not null || field.Max is not null))
            throw invalid(origin,
                $"field '{field.Name}': min/max apply to numeric fields only");
    }

    private static bool isIdentifier(string name) => name is not null && identifierRegex().IsMatch(name);

    private static MtprException invalid(string origin, string reason)
        => new(MtprErrors.InvalidDefinition,
            new ExceptionParameter(origin),
            new ExceptionParameter(reason));
}
