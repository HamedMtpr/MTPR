using System.Text;

namespace Mtpr.Core.Exceptions;

public static class ExceptionExtensions
{
    /// <summary>
    /// Returns the exception itself followed by each of its inner exceptions, outermost first.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
    public static IEnumerable<Exception> GetExceptionWithAllInnerExceptions(this Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.GetTraversedHierarchyNodes(e => e?.InnerException);
    }

    /// <summary>
    /// Builds a text containing message and stack trace of the exception and all its inner exceptions.
    /// Exceptions with an empty or whitespace message are skipped.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
    public static string GetFullMessageStackTrace(this Exception exception)
        => buildFullMessage(exception, withStackTrace: true);

    /// <summary>
    /// Builds a text containing messages of the exception and all its inner exceptions.
    /// Exceptions with an empty or whitespace message are skipped.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
    public static string GetFullMessage(this Exception exception)
        => buildFullMessage(exception, withStackTrace: false);

    private static string buildFullMessage(Exception exception, bool withStackTrace)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var builder = new StringBuilder();

        foreach (var item in exception.GetExceptionWithAllInnerExceptions())
        {
            if (string.IsNullOrWhiteSpace(item.Message))
                continue;

            builder.AppendLine($"Message - {item.Message}");

            if (withStackTrace && !string.IsNullOrWhiteSpace(item.StackTrace))
                builder.AppendLine($"StackTrace - {item.StackTrace}");
        }

        return builder.ToString();
    }
}
