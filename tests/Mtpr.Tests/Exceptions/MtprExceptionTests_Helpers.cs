using Mtpr.Core.Exceptions;

namespace Mtpr.Tests.Exceptions;

public partial class MtprExceptionTests
{
    private const string someCode = "Some someCode";
    private const string someMessage = "Some Message";
    private const string someTwoParamsMessage = "Some Message With Two ExceptionParameters: {0}, {1}";
    private const string someInnerExceptionMessage = "Some Inner Exception";
    private const string someExceptionParameterValue = "Some Parameter";
    private const double anotherExceptionParameterValue = 14.58d;

    private static ExceptionParameter[] twoParams() =>
    [
        new(someExceptionParameterValue),
        new(anotherExceptionParameterValue)
    ];

    private static string expectedTwoParamsMessage() =>
        $"Error code({someCode}): " + string.Format(someTwoParamsMessage,
            someExceptionParameterValue,
            anotherExceptionParameterValue);

    private static void assertPlain(MtprException sut)
    {
        sut.Code.Should().Be(someCode);
        sut.Message.Should().Be($"Error code({someCode}): {someMessage}");
        sut.Parameters.Should().BeNull();
    }

    private static void assertTwoParams(MtprException sut)
    {
        sut.Code.Should().Be(someCode);
        sut.Message.Should().Be(expectedTwoParamsMessage());
        sut.Parameters.Should().NotBeNull();
        sut.Parameters!.Select(p => p.Value).Should().Equal(
            someExceptionParameterValue,
            anotherExceptionParameterValue);
    }
}