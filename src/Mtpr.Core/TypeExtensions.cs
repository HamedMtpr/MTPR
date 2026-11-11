using System.Reflection;

namespace Mtpr.Core;

public static class TypeExtensions
{
    extension(Type type)
    {
        /// <summary>
        /// Returns the first <typeparamref name="TAttribute"/> declared on a public property,
        /// or null when the property does not exist or the attribute is not applied.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute type to look for.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="propertyName"/> is null.</exception>
        public TAttribute? GetAttribute<TAttribute>(string propertyName)
            where TAttribute : Attribute
        {
            return type.GetAttributes<TAttribute>(propertyName)
                .FirstOrDefault();
        }

        /// <summary>
        /// Returns every <typeparamref name="TAttribute"/> declared on a public property.
        /// Empty when the property does not exist or the attribute is not applied.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute type to look for.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="propertyName"/> is null.</exception>
        public IEnumerable<TAttribute> GetAttributes<TAttribute>(string propertyName)
            where TAttribute : Attribute
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(propertyName);

            var propertyInfo = type.GetProperty(propertyName);

            return propertyInfo is null
                ? Array.Empty<TAttribute>()
                : propertyInfo.GetCustomAttributes<TAttribute>(inherit: false);
        }
    }
}
