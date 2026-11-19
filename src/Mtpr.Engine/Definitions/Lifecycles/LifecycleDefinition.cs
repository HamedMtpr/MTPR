using System.Text.Json.Serialization;

namespace Mtpr.Engine.Definitions.Lifecycles;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class LifecycleDefinition
{
    [JsonRequired] public string Field { get; set; } = string.Empty;
    [JsonRequired] public string Initial { get; set; } = string.Empty;
    [JsonRequired] public List<string> States { get; set; } = [];
    [JsonRequired] public List<LifecycleTransition> Transitions { get; set; } = [];
}