namespace Mtpr.Core.Repositories;

public class FilterCriteriaDto
{
    public string? FilterOperation { get; set; }
    public string? PropertyName { get; set; }
    public object? Value { get; set; }
}
