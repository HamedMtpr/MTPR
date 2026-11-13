using Mtpr.Core.Repositories;
using Mtpr.TestUtilities;

namespace Mtpr.Tests.Repositories;

public class QueryTests : UnitTest
{
    [Fact]
    public void GetQuery_Should_Return_Expected()
    {
        // Arrange

        // Act
        var actual = new GetQuery
        {
            Filters =
            [
                new FilterCriteriaDto()
            ],
        };

        // Assert
        actual.Should().BeAssignableTo<BaseQuery>();
    }
    
    [Fact]
    public void GetAllQuery_Should_Return_Expected()
    {
        // Arrange

        // Act
        var actual = new GetAllQuery
        {
            Filters =
            [
                new FilterCriteriaDto()
            ],
        };

        // Assert
        actual.Should().BeAssignableTo<BaseQuery>();
    }
}