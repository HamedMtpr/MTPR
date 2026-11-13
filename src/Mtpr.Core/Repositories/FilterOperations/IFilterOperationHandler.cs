namespace Mtpr.Core.Repositories.FilterOperations;

public interface IFilterOperationHandler
{
    IFilterOperation Handle(string operationFilter);
}