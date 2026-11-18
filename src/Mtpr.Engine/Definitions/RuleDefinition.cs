using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mtpr.Engine.Definitions;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RuleDefinition
{
    public string Trigger { get; set; } = string.Empty;
    public JsonElement Condition { get; set; }
    public RuleAction? Action { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RuleAction
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
