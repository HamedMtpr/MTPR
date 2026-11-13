using System.Collections;
using Mtpr.Core.Repositories.FilterOperations;

namespace Mtpr.Tests.Repositories.QueryExtensions;

public partial class QueryExtensionsTests
{
    public class TestToFiltersCriteriaDataGenerator : IEnumerable<object[]>
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<object[]> GetEnumerator()
        {
            yield return
            [
                nameof(ContainsFilterOperation),
                new ContainsFilterOperation(),
            ];

            yield return
            [
                nameof(EqualFilterOperation),
                new EqualFilterOperation(),
            ];

            yield return
            [
                nameof(GreaterThanFilterOperation),
                new GreaterThanFilterOperation(),
            ];

            yield return
            [
                nameof(GreaterThanOrEqualFilterOperation),
                new GreaterThanOrEqualFilterOperation(),
            ];

            yield return
            [
                nameof(InFilterOperation),
                new InFilterOperation(),
            ];

            yield return
            [
                nameof(LessThanFilterOperation),
                new LessThanFilterOperation(),
            ];
            yield return
            [
                nameof(LessThanOrEqualFilterOperation),
                new LessThanOrEqualFilterOperation(),
            ];
            yield return
            [
                nameof(NotEqualFilterOperation),
                new NotEqualFilterOperation(),
            ];
        }
    }
}