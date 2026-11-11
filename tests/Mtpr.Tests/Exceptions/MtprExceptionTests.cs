using Mtpr.Core.Exceptions;
using Mtpr.TestUtilities;

namespace Mtpr.Tests.Exceptions;

public partial class MtprExceptionTests : UnitTest
{
    #region Constructors with MtprExceptionCode

    [Fact]
    public void Ctor_Code_Only()
    {
        var sut = new MtprException(
            new MtprExceptionCode(someCode, someMessage));

        assertPlain(sut);
        sut.InnerException.Should().BeNull();
    }

    [Fact]
    public void Ctor_Code_And_Inner()
    {
        var inner = new Exception(someInnerExceptionMessage);

        var sut = new MtprException(
            new MtprExceptionCode(someCode, someMessage),
            inner);

        assertPlain(sut);
        sut.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void Ctor_Code_And_Parameters()
    {
        var sut = new MtprException(
            new MtprExceptionCode(someCode, someTwoParamsMessage),
            twoParams());

        assertTwoParams(sut);
        sut.InnerException.Should().BeNull();
    }

    [Fact]
    public void Ctor_Code_Inner_And_Parameters()
    {
        var inner = new Exception("inner");

        var sut = new MtprException(
            new MtprExceptionCode(someCode, someTwoParamsMessage),
            inner, 
            twoParams());

        assertTwoParams(sut);
        sut.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void Ctor_Code_With_Named_Inner_Only()
    {
        var inner = new Exception("inner");

        var sut = new MtprException(
            new MtprExceptionCode(someCode, someMessage),
            innerException: 
            inner);

        assertPlain(sut);
        sut.InnerException.Should().BeSameAs(inner);
    }

    #endregion

    #region Constructors with string code and message

    [Fact]
    public void Ctor_String_Code_And_Message()
    {
        var sut = new MtprException(someCode, someMessage);

        assertPlain(sut);
        sut.InnerException.Should().BeNull();
    }

    [Fact]
    public void Ctor_String_Code_Message_And_Inner()
    {
        var inner = new Exception("inner");

        var sut = new MtprException(someCode, someMessage, inner);

        assertPlain(sut);
        sut.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void Ctor_String_Code_Message_And_Parameters()
    {
        var sut = new MtprException(someCode,
            someTwoParamsMessage,
            twoParams());

        assertTwoParams(sut);
        sut.InnerException.Should().BeNull();
    }

    [Fact]
    public void Ctor_String_Code_Message_Inner_And_Parameters()
    {
        var inner = new Exception("inner");

        var sut = new MtprException(someCode,
            someTwoParamsMessage,
            inner, 
            twoParams());

        assertTwoParams(sut);
        sut.InnerException.Should().BeSameAs(inner);
    }

    #endregion

    #region someCode and message edge cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Code_Without_Value_Should_Be_Undefined_And_Message_Without_Prefix(string? code)
    {
        var sut = new MtprException(new MtprExceptionCode(code, someMessage));

        sut.Code.Should().Be(MtprException.UndefinedCode).And.Be("Undefined Code");
        sut.Message.Should().Be(someMessage);
    }

    [Fact]
    public void Single_Argument_Code_Should_Be_Undefined()
    {
        new MtprException(
            new MtprExceptionCode(someMessage))
            .Code
            .Should()
            .Be("Undefined Code");
    }

    [Fact]
    public void Braces_In_Code_Value_Should_Not_Break_Formatting()
    {
        var sut = new MtprException("{0}", "Hello");

        sut.Message.Should().Be("Error code({0}): Hello");
    }

    [Fact]
    public void Extra_Parameters_Should_Be_Ignored_In_Message_But_Kept()
    {
        var sut = new MtprException(someCode, someMessage, twoParams());

        sut.Message.Should().Be($"Error code({someCode}): {someMessage}");
        sut.Parameters.Should().HaveCount(2);
    }

    [Fact]
    public void Empty_Parameters_Array_Should_Leave_Parameters_Null()
    {
        new MtprException(someCode, someMessage,
            Array.Empty<ExceptionParameter>())
            .Parameters
            .Should()
            .BeNull();
    }

    [Fact]
    public void Should_Not_Be_ApplicationException()
    {
        new MtprException(someCode, someMessage)
            .Should()
            .NotBeAssignableTo<ApplicationException>();
    }

    #endregion

    #region Failures

    [Fact]
    public void Null_ExceptionCode_Should_Throw_ArgumentNullException()
    {
        Action action = () => new MtprException((MtprExceptionCode)null!);

        action.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("mtprExceptionCode");
    }

    [Fact]
    public void Not_Enough_Parameters_Should_Throw_FormatException_With_Inner()
    {
        Action action = () => new MtprException(someCode, someTwoParamsMessage);

        action.Should()
            .Throw<FormatException>()
            .WithMessage("MtprException has not enough parameter.")
            .WithInnerExceptionExactly<FormatException>();
    }

    [Fact]
    public void Null_Parameter_Element_Should_Throw_ArgumentException()
    {
        Action action = () => new MtprException(someCode, someMessage, [null!]);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("parameters");
    }

    [Fact]
    public void Null_Parameters_Array_Should_Be_Treated_As_Empty()
    {
        var sut = new MtprException(someCode,
            someMessage,
            (ExceptionParameter[])null!);

        assertPlain(sut);
    }

    #endregion
}
