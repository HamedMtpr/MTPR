using System.Linq.Expressions;

namespace Mtpr.Core.Repositories.FilterOperations;

public class NotEqualFilterOperation : ComparisonFilterOperation
{
    protected override Expression Compare(Expression property, ConstantExpression value)
    {
        return Expression.NotEqual(property, value);
    }
}
