using System.Linq.Expressions;

namespace Mtpr.Core.Repositories.FilterOperations;

public interface IFilterOperation
{
    Expression GetPredicate(Expression property, object? value);
}
