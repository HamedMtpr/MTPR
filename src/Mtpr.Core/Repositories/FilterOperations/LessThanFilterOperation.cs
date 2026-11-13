using System.Linq.Expressions;

namespace Mtpr.Core.Repositories.FilterOperations;

public class LessThanFilterOperation : ComparisonFilterOperation
{
    protected override Expression Compare(Expression property, ConstantExpression value)
    {
        return Expression.LessThan(property, value);
    }
}
