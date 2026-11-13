using System.Linq.Expressions;

namespace Mtpr.Core.Repositories.FilterOperations;

public class GreaterThanFilterOperation : ComparisonFilterOperation
{
    protected override Expression Compare(Expression property, ConstantExpression value)
    {
        return Expression.GreaterThan(property, value);
    }
}
