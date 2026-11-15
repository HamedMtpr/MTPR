using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using Mtpr.Core.Exceptions;
using Mtpr.Engine.Errors;

namespace Mtpr.Engine.Definitions;

/// <summary>Reads one entity per <c>*.json</c> file from a directory.</summary>
public class JsonDirectoryDefinitionSource(string directory, 
    IEntityDefinitionVerifier verifier)
    : IEntityDefinitionSource
{
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase,
                allowIntegerValues: false)
        }
    };

    public string GetFingerprint()
    {
        if (!Directory.Exists(directory))
            return string.Empty;

        return string.Join("|", files().Select(f =>
        {
            using var stream = openShared(f);
            return $"{f}:{Convert.ToHexString(SHA256.HashData(stream))}";
        }));
    }

    public IReadOnlyList<EntityDefinition> Load()
    {
        if (!Directory.Exists(directory))
            return [];

        var result = new Dictionary<string, EntityDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files())
        {
            var definition = read(file);
            verifier.Verify(definition, Path.GetFileName(file));

            if (!result.TryAdd(definition.Name, definition))
                throw new MtprException(MtprErrors.InvalidDefinition,
                    new ExceptionParameter(Path.GetFileName(file)),
                    new ExceptionParameter($"entity '{definition.Name}' is defined twice"));
        }

        return result.Values.ToList();
    }

    private string[] files()
        => Directory
            .GetFiles(directory, "*.json")
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static EntityDefinition read(string file)
    {
        try
        {
            using var stream = openShared(file);

            return JsonSerializer.Deserialize<EntityDefinition>(stream, options)
                   ?? throw new JsonException("file is empty");
        }
        catch (Exception e) when (e is JsonException or IOException)
        {
            throw new MtprException(MtprErrors.InvalidDefinition,
                e,
                new ExceptionParameter(Path.GetFileName(file)),
                new ExceptionParameter(e.Message));
        }
    }

    private static FileStream openShared(string file)
        => new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
}
