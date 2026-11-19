using System.Text.Json;
using Mtpr.Core.Exceptions;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Errors;
using Mtpr.Engine.Validation;

namespace Mtpr.Engine.Rules;

/// <summary>Interprets bounded, declarative validation rules.</summary>
public static class CreateRuleInterpreter
{
    public static void Verify(EntityDefinition definition, string origin)
    {
        if (definition.Rules is null || definition.Rules.Count > 32)
            throw invalid(origin, "rules must be an array with at most 32 entries");

        foreach (var rule in definition.Rules)
        {
            var count = 0;

            if (rule is null || rule.Trigger is not ("beforeCreate" or 
                    "afterCreate" or 
                    "beforeUpdate" or 
                    "afterUpdate" or 
                    "beforeDelete" or 
                    "afterDelete") ||
                    rule.Action is null ||
                    rule.Action.Type != "reject" || 
                    string.IsNullOrWhiteSpace(rule.Action.Message) ||
                    rule.Action.Message.Length > 500 ||
                    !verifyCondition(rule.Condition, definition, 0, ref count))
                throw invalid(origin, "invalid or unsupported rule");
        }
    }

    private static bool verifyCondition(JsonElement node, 
        EntityDefinition definition,
        int depth,
        ref int count)
    {
        if (++count > 128 || 
                depth > 12 || 
                node.ValueKind != JsonValueKind.Object ||
                !node.TryGetProperty("operator", out var operatorNode) ||
                operatorNode.ValueKind != JsonValueKind.String)
            return false;

        var @operator = operatorNode.GetString();
        if (@operator is "and" or "or")
        {
            if (!hasProperties(node, "operator", "conditions") ||
                    !node.TryGetProperty("conditions", out var conditions) || 
                    conditions.ValueKind != JsonValueKind.Array ||
                    conditions.GetArrayLength() is < 1 or > 32)
                return false;

            foreach (var child in conditions.EnumerateArray())
                if (!verifyCondition(child, definition, depth + 1, ref count))
                    return false;

            return true;
        }

        if (@operator is not ("equal" or
                "notEqual" or 
                "greaterThan" or 
                "greaterThanOrEqual" or 
                "lessThan" or 
                "lessThanOrEqual") ||
                !hasProperties(node, "operator", "left", "right") ||
                !node.TryGetProperty("left", out var left) || 
                !hasProperties(left, "field") ||
                !left.TryGetProperty("field", out var name) ||
                name.ValueKind != JsonValueKind.String ||
                definition.FindField(name.GetString()!) is not { } field ||
                !node.TryGetProperty("right", out var right) || 
                !hasProperties(right, "value") ||
                !right.TryGetProperty("value", out var constant))
            return false;

        if (constant.ValueKind == JsonValueKind.Null)
            return @operator is "equal" or "notEqual";

        if (!RecordValidator.tryConvert(field, constant, out _))
            return false;

        return @operator is "equal" or "notEqual" ||
               field.Type is FieldType.Int or 
                   FieldType.Long or 
                   FieldType.Decimal or 
                   FieldType.Double;
    }

    private static bool hasProperties(JsonElement node, params string[] names)
        => node.ValueKind == JsonValueKind.Object &&
           node.EnumerateObject().Count() == names.Length &&
           node.EnumerateObject().Select(property => property.Name)
               .Distinct(StringComparer.Ordinal)
               .Count() == names.Length &&
           node.EnumerateObject()
               .All(property => 
                   names.Contains(property.Name, StringComparer.Ordinal));

    public static void Execute(EntityDefinition definition,
        IReadOnlyDictionary<string, object?> values,
        string trigger)
    {
        foreach (var rule in 
                 definition.Rules.Where(rule => 
                     rule.Trigger == trigger && 
                     evaluate(rule.Condition, definition, values)))
            throw new MtprException(MtprErrors.RuleRejected, 
                new ExceptionParameter(rule.Action!.Message));
    }

    private static bool evaluate(JsonElement node,
        EntityDefinition definition,
        IReadOnlyDictionary<string, object?> values)
    {
        var @operator = node.GetProperty("operator").GetString();
        
        if (@operator == "and")
            return node.GetProperty("conditions")
                .EnumerateArray()
                .All(child => evaluate(child, definition, values));

        if (@operator == "or")
            return node.GetProperty("conditions")
                .EnumerateArray()
                .Any(child => evaluate(child, definition, values));

        var name = node.GetProperty("left")
            .GetProperty("field")
            .GetString()!;
        var field = definition.FindField(name)!;
        var value = values[field.Name];
        var constant = node
            .GetProperty("right")
            .GetProperty("value");

        object? other = null;
        if (constant.ValueKind != JsonValueKind.Null)
            RecordValidator.tryConvert(field, constant, out other);
        
        if (@operator == "equal") 
            return Equals(value, other);

        if (@operator == "notEqual")
            return !Equals(value, other);

        if (value is null || other is null) 
            return false;

        var comparison = field.Type == FieldType.Double
            ? ((double)value).CompareTo((double)other)
            : Convert.ToDecimal(value)
                .CompareTo(Convert.ToDecimal(other));

        return @operator switch
        {
            "greaterThan" => comparison > 0,
            "greaterThanOrEqual" => comparison >= 0,
            "lessThan" => comparison < 0,
            "lessThanOrEqual" => comparison <= 0,
            _ => false
        };
    }

    private static MtprException invalid(string origin, string reason) => 
        new(MtprErrors.InvalidDefinition,
            new ExceptionParameter(origin),
            new ExceptionParameter(reason));
}
