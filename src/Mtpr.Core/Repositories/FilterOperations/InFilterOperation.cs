using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Mtpr.Core.Repositories.FilterOperations;

public class InFilterOperation : IFilterOperation
{
    private static readonly MethodInfo containsMethod = typeof(Enumerable)
        .GetMethods()
        .Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);

    public Expression GetPredicate(Expression property, object? value)
    {
        if (value is not IEnumerable items || value is string)
            throw new InvalidFilterException($"Operation '{nameof(InFilterOperation)}' requires a collection value.");

        var values = items.Cast<object?>()
            .ToList();
        var array = Array.CreateInstance(property.Type, values.Count);

        for (var i = 0; i < values.Count; i++)
            array.SetValue(FilterValueConverter.Convert(values[i], property.Type), i);

        var method = containsMethod.MakeGenericMethod(property.Type);

        return Expression.Call(method, Expression.Constant(array), property);
    }
}
