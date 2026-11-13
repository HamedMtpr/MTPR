using System.Globalization;

namespace Mtpr.Core.Repositories;

public static class FilterValueConverter
{
    public static object? Convert(object? value, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value is null)
        {
            if (underlyingType.IsValueType && underlyingType == targetType)
                throw new InvalidFilterException($"Null is not a valid value for type '{targetType.Name}'.");

            return null;
        }

        if (underlyingType.IsInstanceOfType(value))
            return value;

        try
        {
            return convertCore(value, underlyingType);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            throw new InvalidFilterException(
                $"Value '{value}' cannot be converted to type '{targetType.Name}'.",
                exception);
        }
    }

    private static object convertCore(object value, Type type)
    {
        if (type.IsEnum)
            return value is string name
                ? Enum.Parse(type, name, true)
                : Enum.ToObject(type, System.Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture));

        if (type == typeof(Guid))
            return Guid.Parse(System.Convert.ToString(value, CultureInfo.InvariantCulture)!);

        if (type == typeof(DateTime))
            return DateTime.Parse(System.Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        if (type == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(System.Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);

        if (type == typeof(DateOnly))
            return DateOnly.Parse(System.Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);

        if (type == typeof(TimeOnly))
            return TimeOnly.Parse(System.Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);

        if (type == typeof(TimeSpan))
            return TimeSpan.Parse(System.Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);

        if (value is double number && isIntegral(type) && number != Math.Truncate(number))
            throw new FormatException($"'{number}' is not an integer.");

        return System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
    }

    private static bool isIntegral(Type type)
    {
        return Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.UInt64;
    }
}
