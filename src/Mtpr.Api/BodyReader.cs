using System.Text.Json;
using Mtpr.Core.Exceptions;
using Mtpr.Engine.Errors;

namespace Mtpr.Api;

public static class BodyReader
{
    public static IReadOnlyDictionary<string, JsonElement> ToInput(JsonElement body)
    {
        if (body.ValueKind is not JsonValueKind.Object)
            throw new MtprException(MtprErrors.InvalidRequestBody);

        var result =
            new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in body.EnumerateObject())
            if (!result.TryAdd(property.Name, property.Value))
                throw new MtprException(MtprErrors.InvalidRequestBody);

        return result;
    }
}
