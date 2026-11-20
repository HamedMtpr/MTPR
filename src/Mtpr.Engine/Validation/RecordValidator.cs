using System.Text.Json;
using Mtpr.Core.Exceptions;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Errors;

namespace Mtpr.Engine.Validation;

public class RecordValidator : IRecordValidator
{
    public IReadOnlyDictionary<string, object?> Validate(EntityDefinition definition,
        IReadOnlyDictionary<string, JsonElement> input,
        bool partial)
    {
        var errors = new List<MtprException>();
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in input.Keys.Where(k => definition.FindField(k) is null))
            errors.Add(new MtprException(MtprErrors.UnknownField,
                new ExceptionParameter(key),
                new ExceptionParameter(definition.Name)));

        foreach (var field in definition.Fields)
        {
            if (field.Computed.ValueKind != JsonValueKind.Undefined)
            {
                if (input.Keys.Any(key => 
                        string.Equals(key, 
                            field.Name,
                            StringComparison.OrdinalIgnoreCase)))
                    errors.Add(new MtprException(
                        MtprErrors.ComputedFieldReadOnly, 
                        new ExceptionParameter(field.Name)));
                continue;
            }

            validateField(field, 
                input, 
                partial,
                values,
                errors);
        }

        if (errors.Count > 0)
            throw new EntityValidationException(errors);

        return values;
    }

    private static void validateField(FieldDefinition field,
        IReadOnlyDictionary<string, JsonElement> input,
        bool partial,
        Dictionary<string, object?> values,
        List<MtprException> errors)
    {
        var found = 
            input.FirstOrDefault(p => 
                string.Equals(p.Key, field.Name, 
                    StringComparison.OrdinalIgnoreCase));

        var present = found.Key is not null;
        var isNull = !present || found.Value.ValueKind is JsonValueKind.Null;

        if (isNull)
        {
            if (field.Required && (!partial || present))
                errors.Add(new MtprException(MtprErrors.FieldRequired,
                    new ExceptionParameter(field.Name)));

            else if (present || !partial)
                values[field.Name] = null;

            return;
        }

        if (!tryConvert(field, found.Value, out var value))
        {
            errors.Add(new MtprException(MtprErrors.FieldTypeMismatch,
                new ExceptionParameter(field.Name),
                new ExceptionParameter(field.Type)));

            return;
        }

        var before = errors.Count;
        checkConstraints(field, value!, errors);

        if (errors.Count == before)
            values[field.Name] = value;
    }

    internal static bool tryConvert(FieldDefinition field, 
        JsonElement element,
        out object? value)
    {
        value = null;
        var kind = element.ValueKind;

        switch (field.Type)
        {
            case FieldType.String when kind is JsonValueKind.String:
                value = element.GetString();
                return true;
            case FieldType.Int when kind is JsonValueKind.Number && 
                                    element.TryGetInt32(out var i):
                value = i;
                return true;
            case FieldType.Long when kind is JsonValueKind.Number && 
                                     element.TryGetInt64(out var l):
                value = l;
                return true;
            case FieldType.Decimal when kind is JsonValueKind.Number && 
                                        element.TryGetDecimal(out var m):
                value = m;
                return true;
            case FieldType.Double when kind is JsonValueKind.Number &&
                                       element.TryGetDouble(out var d) && 
                                       double.IsFinite(d):
                value = d;
                return true;
            case FieldType.Bool when kind is JsonValueKind.True or JsonValueKind.False:
                value = element.GetBoolean();
                return true;
            case FieldType.DateTime when kind is JsonValueKind.String &&
                                         element.TryGetDateTime(out var t):
                value = t.ToUniversalTime();
                return true;
            case FieldType.Guid when kind is JsonValueKind.String && 
                                     element.TryGetGuid(out var g):
                value = g;
                return true;
            default:
                return false;
        }
    }

    internal static void checkConstraints(FieldDefinition field,
        object value, 
        List<MtprException> errors)
    {
        if (value is string text)
        {
            if (field.MaxLength is { } max && text.Length > max)
                errors.Add(error(MtprErrors.FieldTooLong,
                    field.Name, 
                    max));

            if (field.MinLength is { } min && text.Length < min)
                errors.Add(error(MtprErrors.FieldTooShort, 
                    field.Name, 
                    min));

            return;
        }

        if (value is not (int or long or decimal or double))
            return;

        if (value is double floating)
        {
            if (field.Min is { } min && floating < (double)min)
                errors.Add(error(MtprErrors.FieldBelowMinimum, field.Name, min));
            if (field.Max is { } max && floating > (double)max)
                errors.Add(error(MtprErrors.FieldAboveMaximum, field.Name, max));
            return;
        }

        var number = Convert.ToDecimal(value);

        if (field.Min is { } lower && number < lower)
            errors.Add(error(MtprErrors.FieldBelowMinimum, field.Name, lower));

        if (field.Max is { } upper && number > upper)
            errors.Add(error(MtprErrors.FieldAboveMaximum, field.Name, upper));
    }

    private static MtprException error(MtprExceptionCode code, 
        string name,
        object limit) => new(code, 
            new ExceptionParameter(name), 
            new ExceptionParameter(limit));
}