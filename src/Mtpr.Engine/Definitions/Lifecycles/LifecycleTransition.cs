using System.Text.Json.Serialization;

namespace Mtpr.Engine.Definitions.Lifecycles;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class LifecycleTransition
{
    [JsonRequired] public string From { get; set; } = string.Empty;
    [JsonRequired] public string To { get; set; } = string.Empty;
}