using System.Linq.Expressions;

namespace Mtpr.Core.Repositories.FilterOperations;

public class LessThanOrEqualFilterOperation : ComparisonFilterOperation
{
    protected override Expression Compare(Expression property, ConstantExpression value)
    {
        return Expression.LessThanOrEqual(property, value);
    }
}
