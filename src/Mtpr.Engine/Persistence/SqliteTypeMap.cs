using System.Globalization;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Definitions.Fields;

namespace Mtpr.Engine.Persistence;

/// <summary>Maps field types to SQLite storage and back.</summary>
public static class SqliteTypeMap
{
    public static string ColumnType(FieldType type) => type switch
    {
        FieldType.Int or FieldType.Long or FieldType.Bool => "INTEGER",
        FieldType.Double => "REAL",
        _ => "TEXT"
    };

    public static object ToDb(object? value) => value switch
    {
        null => DBNull.Value,
        bool b => b ? 1L : 0L,
        Guid g => g.ToString(),
        DateTime t => t.ToString("O", CultureInfo.InvariantCulture),
        decimal m => m.ToString(CultureInfo.InvariantCulture),
        _ => value
    };

    public static object? FromDb(FieldType type, object value)
    {
        if (value is DBNull)
            return null;

        return type switch
        {
            FieldType.Int => Convert.ToInt32(value, CultureInfo.InvariantCulture),
            FieldType.Long => Convert.ToInt64(value, CultureInfo.InvariantCulture),
            FieldType.Bool => Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0,
            FieldType.Double => Convert.ToDouble(value, CultureInfo.InvariantCulture),
            FieldType.Decimal => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
            FieldType.Guid => Guid.Parse((string)value),
            FieldType.DateTime => DateTime.Parse((string)value, 
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind),
            _ => value
        };
    }
}
