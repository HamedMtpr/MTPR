using System.Linq.Expressions;
using System.Reflection;
using Mtpr.Core.Repositories.FilterOperations;

namespace Mtpr.Core.Repositories;

public static class QueryExtensions
{
    public static IQueryable<T> ApplyFilters<T>(
        this IQueryable<T>? query,
        IEnumerable<FilterCriteria>? filters,
        IReadOnlySet<string>? allowedProperties = null)
    {
        if (query is null)
            return Enumerable.Empty<T>()
                .AsQueryable();

        if (filters is null)
            return query;

        var resultQuery = query;

        foreach (var filter in filters)
            resultQuery = resultQuery.Where(buildPredicate<T>(filter, allowedProperties));

        return resultQuery;
    }

    public static IEnumerable<FilterCriteria> ToFiltersCriteria(this IEnumerable<FilterCriteriaDto> filters,
        IFilterOperationHandler filterOperationHandler)
    {
        return filters.Select(s => s.ToFilterCriteria(filterOperationHandler));
    }

    public static FilterCriteria ToFilterCriteria(this FilterCriteriaDto filterCriteria, 
        IFilterOperationHandler filterOperationHandler)
    {
        if (string.IsNullOrWhiteSpace(filterCriteria.FilterOperation))
            throw new InvalidFilterException("Filter operation is required.");

        return new FilterCriteria
        {
            FilterOperation = filterOperationHandler.Handle(filterCriteria.FilterOperation),
            PropertyName = filterCriteria.PropertyName,
            Value = filterCriteria.Value,
        };
    }

    private static Expression<Func<T, bool>> buildPredicate<T>(FilterCriteria filter, 
        IReadOnlySet<string>? allowedProperties)
    {
        if (filter.FilterOperation is null)
            throw new InvalidFilterException("Filter operation is required.");

        if (string.IsNullOrWhiteSpace(filter.PropertyName))
            throw new InvalidFilterException("Filter property name is required.");

        var propertyInfo = typeof(T).GetProperty(
            filter.PropertyName,
            BindingFlags.Public | BindingFlags.Instance);

        if (propertyInfo is null || (allowedProperties is not null && 
                                     !allowedProperties.Contains(propertyInfo.Name)))
            throw new InvalidFilterException(
                $"Property '{filter.PropertyName}' is not filterable on '{typeof(T).Name}'.");

        var parameter = Expression.Parameter(typeof(T), "e");
        var property = Expression.Property(parameter, propertyInfo);
        var body = filter.FilterOperation.GetPredicate(property, filter.Value);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
