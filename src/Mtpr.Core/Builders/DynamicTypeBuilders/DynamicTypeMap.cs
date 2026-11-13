namespace Mtpr.Core.Builders.DynamicTypeBuilders;

/// <summary>
/// Maps user-facing type names (e.g. "string", "int", "datetime") to CLR types.
/// </summary>
public static class DynamicTypeMap
{
    private static readonly Dictionary<string, Type> map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["string"] = typeof(string),
        ["int"] = typeof(int),
        ["long"] = typeof(long),
        ["decimal"] = typeof(decimal),
        ["double"] = typeof(double),
        ["bool"] = typeof(bool),
        ["boolean"] = typeof(bool),
        ["date"] = typeof(DateTime),
        ["datetime"] = typeof(DateTime),
        ["guid"] = typeof(Guid),
        ["int?"] = typeof(int?),
        ["long?"] = typeof(long?),
        ["decimal?"] = typeof(decimal?),
        ["double?"] = typeof(double?),
        ["bool?"] = typeof(bool?),
        ["datetime?"] = typeof(DateTime?),
        ["guid?"] = typeof(Guid?),
    };

    public static bool TryResolve(string? typeName, out Type type)
    {
        type = null!;
        return typeName is not null && map.TryGetValue(typeName.Trim(), out type!);
    }

    public static Type Resolve(string typeName)
        => TryResolve(typeName, out var t)
            ? t
            : throw new ArgumentException($"Unsupported type name '{typeName}'.", nameof(typeName));
}