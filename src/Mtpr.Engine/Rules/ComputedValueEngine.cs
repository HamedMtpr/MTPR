using System.Text.Json;
using Mtpr.Core.Exceptions;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Definitions.Fields;
using Mtpr.Engine.Errors;
using Mtpr.Engine.Validation;

namespace Mtpr.Engine.Rules;

/// <summary>Evaluates bounded decimal expressions for computed fields.</summary>
public static class ComputedValueEngine
{
    public static void Verify(EntityDefinition definition, string origin)
    {
        foreach (var field in definition.Fields
                     .Where(f => 
                         f.Computed.ValueKind != JsonValueKind.Undefined))
        {
            var count = 0;

            if (field.Type != FieldType.Decimal || 
                field.References is not null ||
                field.Default.ValueKind != JsonValueKind.Undefined ||
                !verifyExpression(field.Computed, 
                    definition, 
                    0,
                    ref count))
                throw new MtprException(MtprErrors.InvalidDefinition, 
                    new ExceptionParameter(origin),
                    new ExceptionParameter(
                        $"computed field '{field.Name}' has an invalid expression or configuration"));
        }
    }

    private static bool verifyExpression(JsonElement node,
        EntityDefinition definition,
        int depth, 
        ref int count)
    {
        if (++count > 64 || 
            depth > 8 ||
            node.ValueKind != JsonValueKind.Object)
            return false;

        if (hasProperties(node, "field") && 
            node.TryGetProperty("field", out var name) &&
            name.ValueKind == JsonValueKind.String && 
            definition.FindField(name.GetString()!) is { } field)
            return field.Computed.ValueKind == JsonValueKind.Undefined &&
                   field.Type is FieldType.Int or 
                       FieldType.Long or
                       FieldType.Decimal;

        if (hasProperties(node, "value") && 
            node.TryGetProperty("value", out var value))
            return value.ValueKind == JsonValueKind.Number && 
                   value.TryGetDecimal(out _);

        return hasProperties(node, 
                   "operator", 
                   "left",
                   "right") &&
               node.TryGetProperty("operator", out var op) && 
               op.ValueKind == JsonValueKind.String &&
               op.GetString() is "add"
                   or "subtract"
                   or "multiply" &&
               verifyExpression(node.GetProperty("left"), 
                   definition, 
                   depth + 1, 
                   ref count) &&
               verifyExpression(node.GetProperty("right"),
                   definition, 
                   depth + 1, 
                   ref count);
    }

    private static bool hasProperties(JsonElement node,
        params string[] names) =>
        node.EnumerateObject().Count() == names.Length && 
        node.EnumerateObject().Select(p => p.Name)
            .Distinct(StringComparer.Ordinal)
            .Count() == names.Length && 
        node.EnumerateObject().All(p => 
            names.Contains(p.Name, StringComparer.Ordinal));

    public static void Apply(EntityDefinition definition, 
        IDictionary<string, object?> values)
    {
        foreach (var field in definition.Fields
                     .Where(f => 
                         f.Computed.ValueKind != JsonValueKind.Undefined))
        {
            decimal? result;
            try
            {
                result = evaluate(field.Computed,
                    definition, 
                    values);
            }
            catch (OverflowException)
            {
                throw failed(field.Name);
            }

            if (result is null && field.Required)
                throw failed(field.Name);

            if (result is not null)
            {
                List<MtprException> errors = new ();
                RecordValidator.checkConstraints(field, result.Value, errors);
                if (errors.Count > 0)
                    throw new EntityValidationException(errors);
            }

            values[field.Name] = result;
        }
    }

    private static decimal? evaluate(JsonElement node,
        EntityDefinition definition, 
        IDictionary<string, object?> values)
    {
        if (node.TryGetProperty("field", 
                out var name))
        {
            var field = definition.FindField(name.GetString()!)!;
            var value = values[field.Name];

            return value is null 
                ? null 
                : Convert.ToDecimal(value);
        }

        if (node.TryGetProperty("value", 
                out var valueNode))
            return valueNode.GetDecimal();

        var left = 
            evaluate(node.GetProperty("left"),
                definition, 
                values);

        var right = 
            evaluate(node.GetProperty("right"),
                definition, 
                values);

        if (left is null || right is null)
            return null;

        return node.GetProperty("operator")
                .GetString() switch
        {
            "add" => left.Value + right.Value,
            "subtract" => left.Value - right.Value,
            "multiply" => left.Value * right.Value,
            _ => throw new InvalidOperationException("Invalid computed operator.")
        };
    }

    private static EntityValidationException failed(string field)
        => new(
        [
                new MtprException(MtprErrors.ComputationFailed, 
                new ExceptionParameter(field))
            ]);
}
