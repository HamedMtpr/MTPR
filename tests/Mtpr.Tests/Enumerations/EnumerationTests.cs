using Mtpr.Core;
using Mtpr.TestUtilities;

namespace Mtpr.Tests.Enumerations;

public partial class EnumerationTests : UnitTest
{
    #region Constructor

    [Fact]
    public void Constructor_With_Value_And_DisplayName_Should_Set_Properties()
    {
        var actual = new SomeEnumeration("1", "One");

        actual.Value.Should().Be("1");
        actual.DisplayName.Should().Be("One");
    }

    [Fact]
    public void Constructor_With_DisplayName_Only_Should_Have_Null_Value()
    {
        var actual = new SomeEnumeration("One");

        actual.Value.Should().BeNull();
        actual.DisplayName.Should().Be("One");
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_Should_Return_DisplayName()
    {
        new SomeEnumeration("1", "One")
            .ToString()
            .Should()
            .Be("One");
    }

    #endregion

    #region Equals

    [Theory]
    [InlineData("1", "One", "1", "One", true)]
    [InlineData("1", "One", "1", "Uno", true)]      
    [InlineData("1", "One", "2", "One", false)]
    [InlineData(null, "A", null, "A", true)]       
    [InlineData(null, "A", null, "B", false)]
    [InlineData(null, "A", "A", "A", false)]
    public void Equals_Should_Return_Expected(string? v1,
        string d1, string? v2,
        string d2,
        bool expected)
    {
        var first = new SomeEnumeration(v1, d1);
        var second = new SomeEnumeration(v2, d2);

        first.Equals(second).Should().Be(expected);
        first.Equals((object)second).Should().Be(expected);
        second.Equals(first).Should().Be(expected);
    }

    [Fact]
    public void Equals_With_Null_Should_Return_False()
    {
        var sut = new SomeEnumeration("1", "One");

        sut.Equals((Enumeration?)null).Should().BeFalse();
        sut.Equals((object?)null).Should().BeFalse();
    }

    [Fact]
    public void Equals_With_Same_Reference_Should_Return_True()
    {
        var sut = new SomeEnumeration("1", "One");

        sut.Equals(sut).Should().BeTrue();
    }

    [Fact]
    public void Equals_With_Different_Type_And_Same_Value_Should_Return_False()
    {
        new SomeEnumeration("1", "One")
            .Equals(new OtherEnumeration("1", "One"))
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Equals_With_Non_Enumeration_Object_Should_Return_False()
    {
        new SomeEnumeration("1", "One")
            .Equals(new object())
            .Should()
            .BeFalse();
    }

    #endregion

    #region GetHashCode

    [Theory]
    [InlineData("1", "One", "1", "Uno")]
    [InlineData(null, "A", null, "A")]
    public void GetHashCode_For_Equal_Objects_Should_Be_Equal(string? v1,
        string d1,
        string? v2,
        string d2)
    {
        new SomeEnumeration(v1, d1).GetHashCode()
            .Should()
            .Be(
                new SomeEnumeration(v2, d2).GetHashCode());
    }

    [Fact]
    public void GetHashCode_For_ValueLess_Objects_With_Different_DisplayName_Should_Differ()
    {
        new SomeEnumeration("A").GetHashCode()
            .Should()
            .NotBe(
                new SomeEnumeration("B").GetHashCode());
    }

    #endregion

    #region CompareTo

    [Theory]
    [InlineData("1", "One", "1", "Uno", 0)]
    [InlineData("1", "One", "2", "One", -1)]
    [InlineData("2", "One", "1", "One", 1)]
    [InlineData(null, "A", "1", "A", -1)]           
    [InlineData("1", "A", null, "A", 1)]
    [InlineData(null, "A", null, "A", 0)]
    [InlineData(null, "A", null, "B", -1)]
    [InlineData(null, "B", null, "A", 1)]
    public void CompareTo_Should_Return_Expected_Sign(string? v1, 
        string d1, 
        string? v2, 
        string d2, 
        int expectedSign)
    {
        var first = new SomeEnumeration(v1, d1);
        var second = new SomeEnumeration(v2, d2);

        Math.Sign(first.CompareTo(second))
            .Should()
            .Be(expectedSign);

        Math.Sign(((IComparable)first)
            .CompareTo(second))
            .Should()
            .Be(expectedSign);
    }

    [Fact]
    public void CompareTo_With_Null_Should_Return_Positive()
    {
        var sut = new SomeEnumeration("1", "One");

        sut.CompareTo((Enumeration?)null).Should().BePositive();
        ((IComparable)sut).CompareTo(null).Should().BePositive();
    }

    [Fact]
    public void CompareTo_With_Non_Enumeration_Object_Should_Throw_ArgumentException()
    {
        var sut = new SomeEnumeration("1", "One");

        var action = () => ((IComparable)sut).CompareTo(new object());

        action.Should().Throw<ArgumentException>().WithParameterName("other");
    }

    [Fact]
    public void CompareTo_With_Different_Enumeration_Type_Should_Throw_ArgumentException()
    {
        var sut = new SomeEnumeration("1", "One");

        var action = () => sut.CompareTo(new OtherEnumeration("1", "One"));

        action.Should().Throw<ArgumentException>()
            .WithParameterName("other");
    }

    #endregion
}
