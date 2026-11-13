using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace Mtpr.Core.Builders.DynamicTypeBuilders;

/// <summary>
/// Builds a CLR type at runtime. Every <see cref="Build"/> call emits a new,
/// independent type from the accumulated definition.
/// </summary>
public class DynamicTypeBuilder : Builder<DynamicTypeDefinition, Type>
{
    private static readonly Regex identifierRegex =
        new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private const MethodAttributes getSetAttribute = MethodAttributes.Public |
                                                     MethodAttributes.SpecialName |
                                                     MethodAttributes.HideBySig;

    private readonly HashSet<string> propertyNames = new(StringComparer.Ordinal);

    public DynamicTypeBuilder(string className)
    {
        validateIdentifier(className, nameof(className));
        With(d => d.ClassName = className);
    }

    public DynamicTypeBuilder WithProperty<T>(string name) => WithProperty(name, typeof(T));

    public DynamicTypeBuilder WithProperty(string name, string typeName)
        => WithProperty(name, DynamicTypeMap.Resolve(typeName));

    public DynamicTypeBuilder WithProperty(string name, Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        validateIdentifier(name, nameof(name));
        if (!propertyNames.Add(name))
            throw new ArgumentException($"Duplicate property '{name}'.", nameof(name));

        With(d => d.Properties.Add((name, type)));
        return this;
    }

    public DynamicTypeBuilder WithProperties<T>(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        foreach (var n in names)
            WithProperty<T>(n);
        return this;
    }

    /// <summary>Emits a new type from the configured definition.</summary>
    protected override Type Build(DynamicTypeDefinition source) => emit(source);

    private static Type emit(DynamicTypeDefinition definition)
    {
        // Unique assembly name: no clash with Mtpr.Core or other dynamic assemblies.
        var assemblyName = new AssemblyName($"Mtpr.Dynamic.{Guid.NewGuid():N}");

        // Run (not RunAndCollect): generated types may be cached for a long time.
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);

        var typeBuilder = moduleBuilder.DefineType(
            definition.ClassName,
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);

        // Public parameterless constructor (needed by Activator, serializers, ORMs).
        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

        foreach (var (name, type) in definition.Properties)
            defineProperty(typeBuilder, name, type);

        return typeBuilder.CreateTypeInfo().AsType();
    }

    private static void defineProperty(TypeBuilder typeBuilder, string name, Type type)
    {
        var fieldBuilder = typeBuilder.DefineField($"m_{name}", type, FieldAttributes.Private);

        var propertyBuilder = typeBuilder.DefineProperty(
            name,
            PropertyAttributes.None,
            type,
            Type.EmptyTypes);

        propertyBuilder.SetGetMethod(createGetAccessorBuilder(name, type, typeBuilder, fieldBuilder));
        propertyBuilder.SetSetMethod(createSetAccessorBuilder(name, type, typeBuilder, fieldBuilder));
    }

    private static void validateIdentifier(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value) || !identifierRegex.IsMatch(value))
            throw new ArgumentException($"'{value}' is not a valid identifier.", paramName);
    }

    private static MethodBuilder createGetAccessorBuilder(string propertyName,
        Type type,
        TypeBuilder typeBuilder,
        FieldBuilder fieldBuilder)
    {
        var mb = typeBuilder.DefineMethod($"get_{propertyName}", getSetAttribute, type, Type.EmptyTypes);

        var il = mb.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, fieldBuilder);
        il.Emit(OpCodes.Ret);

        return mb;
    }

    private static MethodBuilder createSetAccessorBuilder(string propertyName,
        Type type,
        TypeBuilder typeBuilder,
        FieldBuilder fieldBuilder)
    {
        var mb = typeBuilder.DefineMethod($"set_{propertyName}", getSetAttribute, null, [type]);

        var il = mb.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, fieldBuilder);
        il.Emit(OpCodes.Ret);

        return mb;
    }
}
