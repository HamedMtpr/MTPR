using Mtpr.Core.Repositories;
using Mtpr.Core.Repositories.FilterOperations;

namespace Mtpr.Tests.Repositories.FilterBehaviors;

public partial class FilterBehaviorTests
{
    private record Item(int Number, Guid Id, DateTime Date, bool Flag, int? Optional);

    private static readonly Guid firstId = Guid.NewGuid();

    private static readonly Item[] items =
    [
        new(1, firstId,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            true,
            null),
        new(2, Guid.NewGuid(),
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            false, 5),
    ];

    private static List<Item> run(string property, IFilterOperation operation,
        object? value,
        IReadOnlySet<string>? allowed = null)
    {
        return items.AsQueryable()
            .ApplyFilters(
                [new FilterCriteria
                    {
                        PropertyName = property,
                        FilterOperation = operation,
                        Value = value
                    }
                ], 
                allowed)
            .ToList();
    }
}