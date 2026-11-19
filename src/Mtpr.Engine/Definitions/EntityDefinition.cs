using Mtpr.Engine.Definitions.Lifecycles;

namespace Mtpr.Engine.Definitions;

public class EntityDefinition
{
    public string Name { get; set; } = string.Empty;

    public List<FieldDefinition> Fields { get; set; } = [];

    public List<List<string>> Unique { get; set; } = [];

    public List<RuleDefinition> Rules { get; set; } = [];

    public LifecycleDefinition? Lifecycle { get; set; }

    public FieldDefinition? FindField(string name)
        => Fields.FirstOrDefault(f => string.Equals(f.Name,
            name,
            StringComparison.OrdinalIgnoreCase));
}
