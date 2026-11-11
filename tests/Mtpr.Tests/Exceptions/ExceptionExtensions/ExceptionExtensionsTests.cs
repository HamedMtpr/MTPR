using Mtpr.Core.Exceptions;
using Mtpr.TestUtilities;

namespace Mtpr.Tests.Exceptions.ExceptionExtensions;

public partial class ExceptionExtensionsTests : UnitTest
{
    #region GetExceptionWithAllInnerExceptions

    [Fact]
    public void GetExceptionWithAllInnerExceptions_With_Four_InnerExceptions_Should_Return_All_In_Order()
    {
        var expected = createChain();

        var actual = 
            expected.First()
                .GetExceptionWithAllInnerExceptions();

        actual.Should().Equal(expected);
    }

    [Fact]
    public void GetExceptionWithAllInnerExceptions_Without_InnerException_Should_Return_Only_Itself()
    {
        var sut = new Exception("Single");

        sut.GetExceptionWithAllInnerExceptions()
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeSameAs(sut);
    }

    [Fact]
    public void GetExceptionWithAllInnerExceptions_With_Null_Should_Throw_Eagerly()
    {
        var action =
            () => ((Exception)null!).GetExceptionWithAllInnerExceptions();

        action.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("exception");
    }

    #endregion

    #region GetFullMessageStackTrace

    [Fact]
    public void GetFullMessageStackTrace_With_Thrown_Exception_Should_Include_StackTrace_Only_For_Thrown_One()
    {
        var sut = createThrownChain();

        var expected =
            $"Message - Exception 1{NewLine}" +
            $"StackTrace - {sut.StackTrace}{NewLine}" +
            $"Message - Exception 2{NewLine}" +
            $"Message - Exception 3{NewLine}" +
            $"Message - Exception 4{NewLine}" +
            $"Message - Exception 5{NewLine}";

        sut.GetFullMessageStackTrace().Should().Be(expected);
    }

    [Fact]
    public void GetFullMessageStackTrace_With_Unthrown_Exception_Should_Not_Contain_StackTrace()
    {
        var sut = new Exception("Not thrown");

        sut.GetFullMessageStackTrace()
            .Should()
            .Be($"Message - Not thrown{NewLine}");
    }

    [Fact]
    public void GetFullMessageStackTrace_With_Whitespace_StackTrace_Should_Omit_StackTrace_Line()
    {
        var sut = new FakeStackTraceException("Some", "   ");

        sut.GetFullMessageStackTrace()
            .Should()
            .Be($"Message - Some{NewLine}");
    }

    [Fact]
    public void GetFullMessageStackTrace_With_Empty_Message_Should_Skip_That_Exception_Including_StackTrace()
    {
        var sut = new FakeStackTraceException("",
            "some trace",
            new Exception("Inner"));

        sut.GetFullMessageStackTrace()
            .Should()
            .Be($"Message - Inner{NewLine}");
    }

    [Fact]
    public void GetFullMessageStackTrace_With_Null_Should_Throw()
    {
        var action = () => ((Exception)null!).GetFullMessageStackTrace();

        action.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("exception");
    }

    #endregion

    #region GetFullMessage

    [Fact]
    public void GetFullMessage_With_Four_InnerExceptions_Should_Return_All_Messages_Without_StackTrace()
    {
        var sut = createThrownChain();

        var expected = string.Concat(
            Enumerable.Range(1, 5)
                .Select(i => $"Message - Exception {i}{NewLine}"));

        sut.GetFullMessage().Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void GetFullMessage_With_Empty_Or_WhiteSpace_Message_Should_Return_Empty(string message)
    {
        new Exception(message)
            .GetFullMessage()
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void GetFullMessage_With_Empty_Message_In_Middle_Should_Skip_Only_That_One()
    {
        var sut = new Exception("Outer",
            new Exception(" ",
                new Exception("Inner")));

        sut.GetFullMessage()
            .Should()
            .Be($"Message - Outer{NewLine}Message - Inner{NewLine}");
    }

    [Fact]
    public void GetFullMessage_With_Null_Should_Throw()
    {
        var action = () => ((Exception)null!).GetFullMessage();

        action.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("exception");
    }

    #endregion
}