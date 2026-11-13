using System.Collections;
using Mtpr.Core.Repositories;
using Mtpr.Core.Repositories.FilterOperations;

namespace Mtpr.Tests.Repositories.QueryExtensions;

public partial class QueryExtensionsTests
{
    public class TestApplyFiltersDataGenerator : IEnumerable<object[]>
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<object[]> GetEnumerator()
        {
            yield return
            [
                null!,
                Enumerable.Empty<FilterCriteria>(),
                Enumerable.Empty<TestRecord>(),
             ];

            yield return
            [
                Enumerable.Empty<TestRecord>().AsQueryable(),
                null!,
                Enumerable.Empty<TestRecord>(),
             ];

            yield return
            [
                new TestRecord[]
                {
                    new("Some Data", 0),
                    new("Another Data", 1),
                }.AsQueryable(),
                new[]
                {
                    new FilterCriteria
                    {
                        FilterOperation = new ContainsFilterOperation(),
                        PropertyName = nameof(TestRecord.StringData),
                        Value = "Data",
                    },
                },
                new []
                {
                    new TestRecord("Some Data", 0),
                    new TestRecord("Another Data", 1),
                }
            ];

            yield return
            [
                new TestRecord[]
                {
                    new("Some Data", 0),
                    new("Another Data", 1),
                }.AsQueryable(),
                new[]
                {
                    new FilterCriteria
                    {
                        FilterOperation = new EqualFilterOperation(),
                        PropertyName = nameof(TestRecord.StringData),
                        Value = "Some Data",
                    },
                },
                new []{ new TestRecord("Some Data", 0) }
            ];

            yield return
            [
                new TestRecord[]
                {
                    new("Some Data",100),
                    new("Another Data",200),
                }.AsQueryable(),
                new[]
                {
                    new FilterCriteria
                    {
                        FilterOperation = new GreaterThanFilterOperation(),
                        PropertyName = nameof(TestRecord.NumberData),
                        Value = 150,
                    },
                },
                new []{ new TestRecord("Another Data",200) }
            ];

            yield return
            [
                new TestRecord[]
                {
                    new("Some Data",100),
                    new("Another Data",200),
                }.AsQueryable(),
                new[]
                {
                    new FilterCriteria
                    {
                        FilterOperation = new GreaterThanOrEqualFilterOperation(),
                        PropertyName = nameof(TestRecord.NumberData),
                        Value = 100,
                    },
                },
                new []
                {
                    new TestRecord("Some Data",100),
                    new("Another Data",200),
                }
            ];

            yield return
            [
                new TestRecord[]
                {
                    new("Some Data", 10),
                    new("Another Data", 11),
                    new("Third Data", 10),
                    new("Fourth Data", 12),
                    new("Fifth Data", 12),
                }.AsQueryable(),
                new[]
                {
                    new FilterCriteria
                    {
                        FilterOperation = new InFilterOperation(),
                        PropertyName = nameof(TestRecord.NumberData),
                        Value = new[]
                        {
                            10,
                            11,
                        },
                    },
                },
                new []
                {
                    new TestRecord("Some Data", 10),
                    new TestRecord("Another Data", 11),
                    new TestRecord("Third Data", 10),
                }
            ];

            yield return
            [
                new TestRecord[]
                {
                    new("Some Data", 100),
                    new("Another Data", 200),
                }.AsQueryable(),
                new[]
                {
                    new FilterCriteria
                    {
                        FilterOperation = new LessThanFilterOperation(),
                        PropertyName = nameof(TestRecord.NumberData),
                        Value = 150,
                    },
                },
                new []{ new TestRecord("Some Data", 100) }
            ];

            yield return
            [
                new TestRecord[]
                {
                    new("Some Data",100),
                    new("Another Data",200),
                }.AsQueryable(),
                new[]
                {
                    new FilterCriteria
                    {
                        FilterOperation = new LessThanOrEqualFilterOperation(),
                        PropertyName = nameof(TestRecord.NumberData),
                        Value = 200,
                    },
                },
                new []
                {
                    new TestRecord("Some Data",100),
                    new("Another Data",200),
                }
            ];

            yield return
            [
                new TestRecord[]
                {
                    new("Some Data",100),
                    new("Another Data",200),
                }.AsQueryable(),
                new[]
                {
                    new FilterCriteria
                    {
                        FilterOperation = new NotEqualFilterOperation(),
                        PropertyName = nameof(TestRecord.NumberData),
                        Value = 200,
                    },
                },
                new []
                {
                    new TestRecord("Some Data",100),
                }
            ];
        }
    }
}
