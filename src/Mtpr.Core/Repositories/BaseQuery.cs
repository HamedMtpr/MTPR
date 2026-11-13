namespace Mtpr.Core.Repositories;

public class BaseQuery
{
    public IEnumerable<FilterCriteriaDto> Filters { get; set; } = [];
}