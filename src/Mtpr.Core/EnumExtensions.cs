using System.Reflection;

namespace Mtpr.Core;

public static class EnumExtensions
{
    extension(Enum value)
    {
        /// <summary>
        /// Returns the value of <paramref name="attributePropertyName"/> from the
        /// <typeparamref name="TAttribute"/> applied to the enum member,
        /// or null when the member has no such attribute.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="attributePropertyName"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not a defined enum member, or <paramref name="attributePropertyName"/> is not a property of <typeparamref name="TAttribute"/>.</exception>
        public object? GetAttributePropertyValue<TAttribute>(string attributePropertyName)
            where TAttribute : Attribute
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(attributePropertyName);

            var property = typeof(TAttribute).GetProperty(attributePropertyName)
                           ?? throw new ArgumentException("Property not found.", nameof(attributePropertyName));

            var name = Enum.GetName(value.GetType(), value)
                       ?? throw new ArgumentException("Value is not a defined member of its enum type.", nameof(value));

            var attribute = value
                .GetType()
                .GetField(name, BindingFlags.Public | BindingFlags.Static)!
                .GetCustomAttribute<TAttribute>(inherit: false);

            return attribute is null
                ? null
                : property.GetValue(attribute);
        }

        /// <summary>
        /// Typed version of <see cref="GetAttributePropertyValue{TAttribute}"/>.
        /// Returns the default of <typeparamref name="TValue"/> when the member has no such attribute.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="attributePropertyName"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="attributePropertyName"/> is not a property of <typeparamref name="TAttribute"/>.</exception>
        /// <exception cref="InvalidCastException">The property value cannot be converted to <typeparamref name="TValue"/>.</exception>
        public TValue? GetAttributePropertyValue<TAttribute, TValue>(string attributePropertyName)
            where TAttribute : Attribute
        {
            var raw = value.GetAttributePropertyValue<TAttribute>(attributePropertyName);

            return raw is null
                ? default
                : (TValue)raw;
        }
    }

    /// <summary>
    /// Returns the enum member whose <typeparamref name="TAttribute"/> property
    /// <paramref name="attributePropertyName"/> equals <paramref name="value"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="attributePropertyName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="attributePropertyName"/> is not a property of <typeparamref name="TAttribute"/>, or no member matches.</exception>
    public static TEnum GetValue<TEnum, TAttribute>(this object value, string attributePropertyName)
        where TEnum : struct, Enum
        where TAttribute : Attribute
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(attributePropertyName);

        var property = typeof(TAttribute).GetProperty(attributePropertyName)
            ?? throw new ArgumentException("Property not found.", nameof(attributePropertyName));

        foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var attribute = field.GetCustomAttribute<TAttribute>(inherit: false);

            if (attribute is null)
                continue;

            if (Equals(property.GetValue(attribute), value))
                return (TEnum)field.GetValue(null)!;
        }

        throw new ArgumentException("Not found.", nameof(value));
    }
}
