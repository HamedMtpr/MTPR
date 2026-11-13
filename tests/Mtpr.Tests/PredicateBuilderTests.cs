using System.Linq.Expressions;
using Mtpr.Core.Builders;
using Mtpr.TestUtilities;

namespace Mtpr.Tests;

public class PredicateBuilderTests : UnitTest
{
    [Fact]
    public void True_Should_Return_Expected()
    {
        // Arrange

        // Act
        var actual = PredicateBuilder.True<object>();

        // Assert
        actual.Should().BeAssignableTo<Expression<Func<object, bool>>>();
        actual
            .Compile()
            .Invoke(new object())
            .Should()
            .BeTrue();
    }

    [Fact]
    public void False_Should_Return_Expected()
    {
        // Arrange

        // Act
        var actual = PredicateBuilder.False<object>();

        // Assert
        actual.Should().BeAssignableTo<Expression<Func<object, bool>>>();
        actual
            .Compile()
            .Invoke(new object())
            .Should()
            .BeFalse();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void Or_With_True_Expression_Should_Return_Expected(bool value, bool expected)
    {
        // Arrange

        // Act
        var actual = PredicateBuilder
            .True<bool>()
            .Or(o => o);

        // Assert
        actual.Should().BeAssignableTo<Expression<Func<bool, bool>>>();
        actual
            .Compile()
            .Invoke(value)
            .Should()
            .Be(expected);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Or_With_False_Expression_Should_Return_Expected(bool value, bool expected)
    {
        // Arrange

        // Act
        var actual = PredicateBuilder
            .False<bool>()
            .Or(o => o);

        // Assert
        actual.Should().BeAssignableTo<Expression<Func<bool, bool>>>();
        actual
            .Compile()
            .Invoke(value)
            .Should()
            .Be(expected);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void And_With_True_Expression_Should_Return_Expected(bool value, bool expected)
    {
        // Arrange

        // Act
        var actual = PredicateBuilder
            .True<bool>()
            .And(a => a);

        // Assert
        actual.Should().BeAssignableTo<Expression<Func<bool, bool>>>();
        actual
            .Compile()
            .Invoke(value)
            .Should()
            .Be(expected);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void And_With_False_Expression_Should_Return_Expected(bool value, bool expected)
    {
        // Arrange

        // Act
        var actual = PredicateBuilder
            .False<bool>()
            .And(a => a);

        // Assert
        actual.Should().BeAssignableTo<Expression<Func<bool, bool>>>();
        actual
            .Compile()
            .Invoke(value)
            .Should()
            .Be(expected);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(500, true)]
    [InlineData(50, false)]
    public void Or_With_Two_Predicates_Should_Combine_Them(int value, bool expected)
    {
        // Arrange
        Expression<Func<int, bool>> isNegative = x => x < 0;
        Expression<Func<int, bool>> isLarge = x => x > 100;

        // Act
        var actual = isNegative.Or(isLarge);

        // Assert
        actual
            .Compile()
            .Invoke(value)
            .Should()
            .Be(expected);
    }

    [Theory]
    [InlineData(4, true)]
    [InlineData(3, false)]
    [InlineData(-4, false)]
    public void And_With_Two_Predicates_Should_Combine_Them(int value, bool expected)
    {
        // Arrange
        Expression<Func<int, bool>> isPositive = x => x > 0;
        Expression<Func<int, bool>> isEven = x => x % 2 == 0;

        // Act
        var actual = isPositive.And(isEven);

        // Assert
        actual
            .Compile()
            .Invoke(value)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void Or_With_Null_First_Expression_Should_Throw()
    {
        // Arrange
        Expression<Func<int, bool>> expr1 = null!;

        // Act
        var actual = () => expr1.Or(x => x > 0);

        // Assert
        actual
            .Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("expr1");
    }

    [Fact]
    public void Or_With_Null_Second_Expression_Should_Throw()
    {
        // Arrange
        Expression<Func<int, bool>> expr1 = x => x > 0;

        // Act
        var actual = () => expr1.Or(null!);

        // Assert
        actual
            .Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("expr2");
    }

    [Fact]
    public void And_With_Null_First_Expression_Should_Throw()
    {
        // Arrange
        Expression<Func<int, bool>> expr1 = null!;

        // Act
        var actual = () => expr1.And(x => x > 0);

        // Assert
        actual
            .Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("expr1");
    }

    [Fact]
    public void And_With_Null_Second_Expression_Should_Throw()
    {
        // Arrange
        Expression<Func<int, bool>> expr1 = x => x > 0;

        // Act
        var actual = () => expr1.And(null!);

        // Assert
        actual
            .Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("expr2");
    }

    [Fact]
    public void Or_Should_Not_Produce_Invocation_Expression()
    {
        // Arrange
        Expression<Func<int, bool>> isNegative = x => x < 0;
        Expression<Func<int, bool>> isLarge = y => y > 100;

        // Act
        var actual = isNegative.Or(isLarge);

        // Assert
        actual.ToString().Should().NotContain("Invoke");
        actual.Parameters.Should().HaveCount(1);
    }

    [Fact]
    public void And_Should_Not_Produce_Invocation_Expression()
    {
        // Arrange
        Expression<Func<int, bool>> isPositive = x => x > 0;
        Expression<Func<int, bool>> isEven = y => y % 2 == 0;

        // Act
        var actual = isPositive.And(isEven);

        // Assert
        actual.ToString().Should().NotContain("Invoke");
        actual.Parameters.Should().HaveCount(1);
    }

    [Fact]
    public void Chained_Combinations_Should_Be_Translatable_By_Queryable()
    {
        // Arrange
        var source = new[] { -5, 2, 3, 50, 200 }.AsQueryable();

        var predicate = PredicateBuilder
            .False<int>()
            .Or(x => x < 0)
            .Or(x => x > 100)
            .And(x => x != 200);

        // Act
        var actual = source
            .Where(predicate)
            .ToList();

        // Assert
        actual.Should().Equal(-5);
    }
}
