using Mtpr.Core.Exceptions;
using Mtpr.TestUtilities;

namespace Mtpr.Tests.Exceptions;

public class MtprExceptionCodeTests : UnitTest
{
    [Theory]
    [InlineData("1", "Some Message", "1", "Some Message", true)]
    [InlineData("1", "Some Message", "1", "Another Message", true)]
    [InlineData("1", "Some Message", "2", "Another Message", false)]
    public void Equals_Should_Return_Expected(string code1, string message1,
        string code2, string message2, bool expected)
    {
        MtprExceptionCode a = new(code1, message1);
        MtprExceptionCode b = new(code2, message2);

        a.Equals(b).Should().Be(expected);
    }

    [Fact]
    public void Equals_With_Same_Value_Should_Have_Same_HashCode()
    {
        new MtprExceptionCode("1", "A")
            .GetHashCode()
            .Should()
            .Be(new MtprExceptionCode("1", "B").GetHashCode());
    }

    [Fact]
    public void CompareTo_With_Equal_Codes_Should_Return_Zero()
    {
        new MtprExceptionCode("1", "A")
            .CompareTo(new MtprExceptionCode("1", "A"))
            .Should()
            .Be(0);
    }

    [Fact]
    public void CompareTo_With_Smaller_Code_Should_Return_Negative_And_Reverse_Positive()
    {
        MtprExceptionCode a = new("1", "A");
        MtprExceptionCode b = new("2", "B");

        a.CompareTo(b).Should().BeNegative();
        b.CompareTo(a).Should().BePositive();
    }

    [Fact]
    public void Equals_With_Non_Enumeration_Object_Should_Return_False()
    {
        new MtprExceptionCode("Some Code", "Some Message")
            .Equals(new object())
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Equals_With_Null_Should_Return_False()
    {
        new MtprExceptionCode("1", "A")
            .Equals(null)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void ToString_Should_Return_DisplayName()
    {
        new MtprExceptionCode("Some Code", "Some Message")
            .ToString()
            .Should()
            .Be("Some Message");
    }

    [Fact]
    public void Constructor_With_Message_Only_Should_Have_Null_Value_And_DisplayName()
    {
        var sut = new MtprExceptionCode("Only Message");

        sut.Value.Should().BeNull();
        sut.DisplayName.Should().Be("Only Message");
    }

    [Fact]
    public void ImplicitString_With_Code_Should_Include_Code_And_Message()
    {
        string actual = new MtprExceptionCode("42", "Some Message");

        actual.Should().Be("Error code(42): Some Message");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ImplicitString_Without_Code_Should_Return_Only_Message(string? code)
    {
        string actual = new MtprExceptionCode(code, "Some Message");

        actual.Should().Be("Some Message");
    }

    [Fact]
    public void ImplicitString_With_Null_Should_Throw_With_ParamName()
    {
        MtprExceptionCode? sut = null;

        Action action = () => { string _ = sut!; };

        action.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("exceptionCode");
    }
}
