using System.Text.Json;
using Mtpr.Engine.Definitions;

namespace Mtpr.Engine.Validation;

public interface IRecordValidator
{
    /// <summary>Validates raw input and returns typed values keyed by field name.</summary>
    /// <param name="definition">The entity definition specifying the fields, types, and validation constraints.</param>
    /// <param name="input">The raw JSON values keyed by field name to validate against the definition.</param>
    /// <param name="partial">When true, required fields may be omitted (PATCH).</param>
    /// <exception cref="EntityValidationException">Input violates the definition.</exception>
    IReadOnlyDictionary<string, object?> Validate(EntityDefinition definition,
        IReadOnlyDictionary<string, JsonElement> input,
        bool partial);
}
