namespace Mtpr.Core.Exceptions;

public class MtprExceptionCode : Enumeration
{
    public MtprExceptionCode(string message) : base(message)
    {
    }

    public MtprExceptionCode(string? code, string message) : base(code, message)
    {
    }

    /// <exception cref="ArgumentNullException"><paramref name="exceptionCode"/> is null.</exception>
    public static implicit operator string(MtprExceptionCode? exceptionCode)
    {
        ArgumentNullException.ThrowIfNull(exceptionCode);

        return string.IsNullOrWhiteSpace(exceptionCode.Value)
            ? exceptionCode.DisplayName
            : $"Error code({exceptionCode.Value}): {exceptionCode.DisplayName}";
    }
}
