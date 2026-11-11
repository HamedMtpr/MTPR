using Mtpr.TestUtilities;

namespace Mtpr.Tests.Exceptions.ExceptionExtensions;

public partial class ExceptionExtensionsTests : UnitTest
{
    private static readonly string NewLine = Environment.NewLine;

    private sealed class FakeStackTraceException(
        string message,
        string? stackTrace,
        Exception? inner = null)
        : Exception(message, inner)
    {
        public override string? StackTrace => stackTrace;
    }

    private static Exception[] createChain()
    {
        var e5 = new Exception("Exception 5");
        var e4 = new Exception("Exception 4", e5);
        var e3 = new Exception("Exception 3", e4);
        var e2 = new Exception("Exception 2", e3);
        var e1 = new Exception("Exception 1", e2);

        return [e1, e2, e3, e4, e5];
    }

    private static Exception createThrownChain()
    {
        try
        {
            throw createChain()[0];
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}