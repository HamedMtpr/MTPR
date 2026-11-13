using Mtpr.Core.Exceptions;

namespace Mtpr.Core.Repositories;

public class InvalidFilterException : MtprException
{
    public const string ErrorCode = "InvalidFilter";

    // The message is passed as a parameter so braces inside it are never treated as format items.
    private const string messageFormat = "{0}";

    public InvalidFilterException(string message)
        : base(ErrorCode, messageFormat, new ExceptionParameter(message))
    {
    }

    public InvalidFilterException(string message, Exception innerException)
        : base(ErrorCode, messageFormat, innerException, new ExceptionParameter(message))
    {
    }
}
