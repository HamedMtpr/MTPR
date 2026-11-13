using System.Linq.Expressions;

namespace Mtpr.Core.Repositories.FilterOperations;

public abstract class ComparisonFilterOperation : IFilterOperation
{
    public Expression GetPredicate(Expression property, object? value)
    {
        var convertedValue = FilterValueConverter.Convert(value, property.Type);
        var constant = Expression.Constant(convertedValue, property.Type);

        try
        {
            return Compare(property, constant);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidFilterException(
                $"Operation '{GetType().Name}' is not supported for type '{property.Type.Name}'.",
                exception);
        }
    }

    protected abstract Expression Compare(Expression property, ConstantExpression value);
}
