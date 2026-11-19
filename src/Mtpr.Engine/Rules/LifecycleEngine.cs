using System.Text.Json;
using Mtpr.Core.Exceptions;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Errors;
using Mtpr.Engine.Validation;

namespace Mtpr.Engine.Rules;

/// <summary>Validates lifecycle definitions and state changes.</summary>
public static class LifecycleEngine
{
    public static void Verify(EntityDefinition definition, 
        string origin)
    {
        if (definition.Lifecycle is not { } lifecycle)
            return;

        var field = definition.FindField(lifecycle.Field);

        if (field is null || field.Type != FieldType.String || 
            !field.Required ||
            lifecycle.States is null || 
            lifecycle.States.Count is < 1 or > 128 ||
            lifecycle.Transitions is null ||
            lifecycle.Transitions.Count > 256 ||
            lifecycle.States.Any(string.IsNullOrWhiteSpace) ||
            lifecycle.States
                .Distinct(StringComparer.Ordinal)
                .Count() != lifecycle.States.Count ||
            !lifecycle.States.Contains(lifecycle.Initial, 
                StringComparer.Ordinal))
            throw invalid(origin, 
                "lifecycle requires a required string field and distinct nonempty states including the initial state");

        foreach (var state in lifecycle.States)
        {
            var errors = new List<MtprException>();
            RecordValidator.checkConstraints(field, state, errors);

            if (errors.Count != 0)
                throw invalid(origin, 
                    "lifecycle states must satisfy field constraints");
        }

        if (field.Default.ValueKind != JsonValueKind.Undefined &&
            (field.Default.ValueKind != JsonValueKind.String || field.Default.GetString() != lifecycle.Initial))
            throw invalid(origin, "lifecycle field default must equal the initial state");

        var seen = new HashSet<(string, string)>();

        foreach (var transition in lifecycle.Transitions)
            if (!lifecycle.States.Contains(transition.From, StringComparer.Ordinal) ||
                !lifecycle.States.Contains(transition.To, StringComparer.Ordinal) ||
                !seen.Add((transition.From, transition.To)))
                throw invalid(origin, 
                    "lifecycle transitions must be unique and reference defined states");
    }

    public static void Execute(EntityDefinition definition,
        string trigger,
        IReadOnlyDictionary<string, object?> record, 
        IReadOnlyDictionary<string, object?>? previous)
    {
        if (definition.Lifecycle is not { } lifecycle || 
            trigger is not ("beforeCreate" or "beforeUpdate"))
            return;

        var name = definition.FindField(lifecycle.Field)!.Name;

        if (record[name] is not string next ||
            !lifecycle.States.Contains(next, StringComparer.Ordinal))
            throw rejected(name);

        if (trigger == "beforeCreate")
        {
            if (!string.Equals(next, lifecycle.Initial, StringComparison.Ordinal))
                throw rejected(name);

            return;
        }

        if (previous![name] is not string old ||
            !lifecycle.States.Contains(old, StringComparer.Ordinal) ||
            (old != next && 
             !lifecycle.Transitions.Any(t => 
                 t.From == old && t.To == next)))
            throw rejected(name);
    }

    private static MtprException invalid(string origin, string reason) =>
        new(MtprErrors.InvalidDefinition,
        new ExceptionParameter(origin), 
        new ExceptionParameter(reason));

    private static MtprException rejected(string field) => 
        new(MtprErrors.InvalidTransition,
            new ExceptionParameter(field));
}
