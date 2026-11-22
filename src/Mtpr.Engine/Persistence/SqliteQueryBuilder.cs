using System.Globalization;
using Microsoft.Data.Sqlite;
using Mtpr.Core.Exceptions;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Definitions.Fields;
using Mtpr.Engine.Errors;

namespace Mtpr.Engine.Persistence;

internal static class SqliteQueryBuilder
{
    public static HashSet<string>? Build(SqliteCommand command,
        EntityDefinition definition,
        RecordQuery query)
    {
        HashSet<string>? selected = null;
        if (query.Fields is not null)
        {
            selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in query.Fields.Split(','))
            {
                var (canonical, _) = resolve(definition, name);
                if (!selected.Add(canonical)) throw invalid("duplicate selected field");
            }
        }

        var where = new List<string>();
        var parameters = 0;

        foreach (var filter in query.Filters)
            where.Add(condition(command,
                definition, 
                filter, 
                ref parameters));

        if (query.Scope is not null)
            where.Add(scope(command,
                definition,
                query.Scope, 
                ref parameters));

        var order = new List<string>();
        if (query.Sort is not null)
            foreach (var item in query.Sort.Split(','))
            {
                var descending = item.StartsWith('-');
                var (name, type) = resolve(definition, descending ? item[1..] : item);
                order.Add(expression(name, type) + (descending ? " DESC" : " ASC"));
            }
        order.Add("_rowid_ ASC");
        command.CommandText = $"SELECT * FROM \"{definition.Name}\"" +
            (where.Count == 0 ? "" : " WHERE " + string.Join(" AND ", where)) +
            " ORDER BY " + string.Join(", ", order) + " LIMIT $take OFFSET $skip";
        return selected;
    }

    /// <summary>Row scope: groups are OR-ed, filters inside a group are AND-ed; no group matches nothing.</summary>
    private static string scope(SqliteCommand command,
        EntityDefinition definition,
        IReadOnlyList<IReadOnlyList<string>> groups,
        ref int parameters)
    {
        if (groups.Count == 0)
            return "0 = 1";

        var parts = new List<string>();

        foreach (var group in groups)
        {
            var conditions = new List<string>();

            foreach (var filter in group)
                conditions.Add(condition(command, 
                    definition, 
                    filter,
                    ref parameters));

            parts.Add(conditions.Count == 0
                ? "0 = 1"
                : "(" + string.Join(" AND ", conditions) + ")");
        }

        return "(" + string.Join(" OR ", parts) + ")";
    }

    private static string condition(SqliteCommand command,
        EntityDefinition definition,
        string filter,
        ref int parameters)
    {
        var parts = filter.Split(':', 3);
        if (parts.Length != 3) 
            throw invalid("filter must be field:operator:value");

        var (name, type) = resolve(definition, parts[0]);
        var column = expression(name, type);
        var op = parts[1].ToLowerInvariant();

        if (op is "isnull" or "notnull")
        {
            if (parts[2].Length != 0) 
                throw invalid("null filters have no value");

            return $"{column} IS {(op == "notnull" ? "NOT " : "")}NULL";
        }

        if (op == "contains" && type != FieldType.String)
            throw invalid("contains requires a string field");

        var sqlOp = op switch
        {
            "eq" => "=",
            "ne" => "<>",
            "gt" => ">",
            "ge" => ">=",
            "lt" => "<",
            "le" => "<=",
            "contains" => "contains",
            _ => throw invalid("unknown filter operator")
        };

        var parameter = "$filter" + parameters++;
        command.Parameters.AddWithValue(parameter, 
            SqliteTypeMap.ToDb(parse(type, parts[2])));

        return sqlOp == "contains"
            ? $"instr({column}, {parameter}) > 0"
            : $"{column} {sqlOp} {parameter}";
    }

    private static (string, FieldType) resolve(EntityDefinition definition, string name)
    {
        if (name.Equals("id", StringComparison.OrdinalIgnoreCase)) return ("id", FieldType.Guid);
        var field = definition.FindField(name) ?? throw invalid($"unknown field '{name}'");
        return (field.Name, field.Type);
    }

    private static string expression(string name, FieldType type)
        => type == FieldType.Decimal ? $"\"{name}\" COLLATE MTPR_DECIMAL" : $"\"{name}\"";

    private static object parse(FieldType type, string value)
    {
        var style = NumberStyles.Number;
        var culture = CultureInfo.InvariantCulture;
        return type switch
        {
            FieldType.String => value,
            FieldType.Int when int.TryParse(value, NumberStyles.Integer, culture, out var i) => i,
            FieldType.Long when long.TryParse(value, NumberStyles.Integer, culture, out var l) => l,
            FieldType.Decimal when decimal.TryParse(value, style, culture, out var m) => m,
            FieldType.Double when double.TryParse(value, NumberStyles.Float, culture, out var d) && double.IsFinite(d) => d,
            FieldType.Bool when bool.TryParse(value, out var b) => b,
            FieldType.Guid when Guid.TryParse(value, out var g) => g,
            FieldType.DateTime when DateTime.TryParse(value, culture, DateTimeStyles.RoundtripKind, out var t) => t.ToUniversalTime(),
            _ => throw invalid("filter value does not match field type")
        };
    }

    private static MtprException invalid(string reason)
        => new(MtprErrors.InvalidQuery, new ExceptionParameter(reason));
}
