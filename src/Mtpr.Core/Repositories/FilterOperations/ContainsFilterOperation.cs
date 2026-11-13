using System.Linq.Expressions;
using System.Reflection;

namespace Mtpr.Core.Repositories.FilterOperations;

public class ContainsFilterOperation : IFilterOperation
{
    private static readonly MethodInfo ContainsMethod = typeof(string)
        .GetMethod(nameof(string.Contains), [typeof(string)])!;

    public Expression GetPredicate(Expression property, object? value)
    {
        if (property.Type != typeof(string))
            throw new InvalidFilterException($"Operation '{nameof(ContainsFilterOperation)}' is only supported for string properties, not '{property.Type.Name}'.");

        var text = (string?)FilterValueConverter.Convert(value, typeof(string))
            ?? throw new InvalidFilterException($"Operation '{nameof(ContainsFilterOperation)}' requires a non-null value.");

        return Expression.Call(property, ContainsMethod, Expression.Constant(text));
    }
}
