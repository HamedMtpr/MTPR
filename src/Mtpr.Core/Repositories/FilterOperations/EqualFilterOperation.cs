using System.Linq.Expressions;

namespace Mtpr.Core.Repositories.FilterOperations;

public class EqualFilterOperation : ComparisonFilterOperation
{
    protected override Expression Compare(Expression property, ConstantExpression value)
    {
        return Expression.Equal(property, value);
    }
}
