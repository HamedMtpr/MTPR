namespace Mtpr.Core.Repositories.FilterOperations;

public class FilterOperationHandler : IFilterOperationHandler
{
    private readonly IReadOnlyDictionary<string, IFilterOperation> _operations;

    public FilterOperationHandler(IEnumerable<IFilterOperation> operations)
    {
        _operations = operations.ToDictionary(
            o => o.GetType().Name,
            StringComparer.OrdinalIgnoreCase);
    }

    public IFilterOperation Handle(string operationFilter)
    {
        if (_operations.TryGetValue(operationFilter, out var operation))
            return operation;

        throw new InvalidFilterException($"Filter operation '{operationFilter}' is not supported.");
    }
}
