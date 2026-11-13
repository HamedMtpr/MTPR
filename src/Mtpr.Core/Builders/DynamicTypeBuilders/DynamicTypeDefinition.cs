namespace Mtpr.Core.Builders.DynamicTypeBuilders;

/// <summary>
/// Mutable description of the type to emit. This is the object that
/// <see cref="Builder{TSource, TResult}"/> assembles inside <see cref="DynamicTypeBuilder"/>.
/// </summary>
public class DynamicTypeDefinition
{
    public string ClassName { get; set; } = string.Empty;

    public List<(string Name, Type Type)> Properties { get; } = [];
}