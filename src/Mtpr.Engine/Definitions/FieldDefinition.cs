using System.Text.Json.Serialization;

namespace Mtpr.Engine.Definitions;

public class FieldDefinition
{
    public string Name { get; set; } = string.Empty;

    [JsonRequired]
    public FieldType Type { get; set; }

    public bool Required { get; set; }

    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }

    public decimal? Min { get; set; }

    public decimal? Max { get; set; }
}
