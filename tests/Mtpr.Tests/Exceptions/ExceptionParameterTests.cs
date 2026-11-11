using Mtpr.Core.Exceptions;
using Mtpr.TestUtilities;

namespace Mtpr.Tests.Exceptions;

public class ExceptionParameterTests : UnitTest
{
    [Fact]
    public void Constructor_With_Value_Only_Should_Have_Null_AlternateValue()
    {
        var sut = new ExceptionParameter("v");

        sut.Value.Should().Be("v");
        sut.AlternateValue.Should().BeNull();
    }

    [Fact]
    public void Constructor_With_Both_Should_Keep_Both()
    {
        var sut = new ExceptionParameter(1, "one");

        sut.Value.Should().Be(1);
        sut.AlternateValue.Should().Be("one");
    }

    [Fact]
    public void Equals_With_Same_Values_Should_Be_True_And_Same_HashCode()
    {
        var a = new ExceptionParameter("v", 2);
        var b = new ExceptionParameter("v", 2);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_With_Different_AlternateValue_Should_Be_False()
    {
        new ExceptionParameter("v", 1)
            .Should()
            .NotBe(new ExceptionParameter("v", 2));
    }

    [Fact]
    public void With_Expression_Should_Copy_And_Change_Only_Given_Property()
    {
        var a = new ExceptionParameter("v", 1);

        var b = a with { AlternateValue = 2 };

        b.Value.Should().Be("v");
        b.AlternateValue.Should().Be(2);
        a.AlternateValue.Should().Be(1);
    }
}
