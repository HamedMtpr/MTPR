using Mtpr.Core.Repositories.FilterOperations;

namespace Mtpr.Core.Repositories;

public class FilterCriteria
{
    public IFilterOperation? FilterOperation { get; set; }
    public string? PropertyName { get; set; }
    public object? Value { get; set; }
}