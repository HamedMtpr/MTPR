using Mtpr.Core.Repositories;
using Mtpr.Core.Repositories.FilterOperations;
using Mtpr.TestUtilities;

namespace Mtpr.Tests.Repositories.QueryExtensions;

public partial class QueryExtensionsTests : UnitTest
{
    [Theory, ClassData(typeof(TestApplyFiltersDataGenerator))]
    public void ApplyFilters_Should_Return_Expected(
        IQueryable<TestRecord> source, 
        IEnumerable<FilterCriteria> filters, 
        IEnumerable<TestRecord> expected)
    {
        // Arrange

        // Act
        IEnumerable<TestRecord> actual = 
            source.ApplyFilters(filters)
                .ToList();

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ApplyFilters_With_Null_Query_Source_Should_Return_Expected()
    {
        // Arrange
        IQueryable<TestRecord>? source = null;
        // Act
        IEnumerable<TestRecord> actual = source.ApplyFilters([])
            .ToList();

        // Assert
        actual.Should().BeEquivalentTo(Enumerable.Empty<TestRecord>());
    }

    [Theory, ClassData(typeof(TestToFiltersCriteriaDataGenerator))]
    public void ToFiltersCriteria_Should_Return_Expected(string nameOfFilterOperation, 
        IFilterOperation filterOperation)
    {
        // Arrange
        var filterOperationHandler = Substitute.For<IFilterOperationHandler>();
        filterOperationHandler.Handle(nameOfFilterOperation)
            .Returns(filterOperation);

        IEnumerable<FilterCriteriaDto> sut =
        [
            new()
            {
                FilterOperation = nameOfFilterOperation,
                PropertyName = "Data",
                Value = "Some Data",
            },
        ];

        IEnumerable<FilterCriteria> expected =
        [
            new()
            {
                FilterOperation = filterOperation,
                PropertyName = "Data",
                Value = "Some Data",
            },
        ];

        // Act
        IEnumerable<FilterCriteria> actual = 
            sut.ToFiltersCriteria(filterOperationHandler);

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }
}