using System.Linq.Expressions;

namespace Mtpr.Core.Repositories.FilterOperations;

public class GreaterThanOrEqualFilterOperation : ComparisonFilterOperation
{
    protected override Expression Compare(Expression property, ConstantExpression value)
    {
        return Expression.GreaterThanOrEqual(property, value);
    }
}
