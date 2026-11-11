namespace Mtpr.Core.Exceptions;

public class MtprException : Exception, IMtprException
{
    public const string UndefinedCode = "Undefined Code";

    public string Code { get; }

    /// <summary>Null when no parameter was supplied.</summary>
    public IList<ExceptionParameter>? Parameters { get; }

    public MtprException(MtprExceptionCode mtprExceptionCode, 
        params ExceptionParameter[] parameters)
        : this(mtprExceptionCode, null, parameters)
    {
    }

    public MtprException(MtprExceptionCode mtprExceptionCode,
        Exception? innerException,
        params ExceptionParameter[] parameters)
        : base(buildMessage(mtprExceptionCode, parameters), innerException)
    {
        Code = string.IsNullOrWhiteSpace(mtprExceptionCode.Value)
            ? UndefinedCode 
            : mtprExceptionCode.Value;

        if (parameters is { Length: > 0 })
            Parameters = parameters.ToList();
    }

    public MtprException(string code, 
        string message, 
        params ExceptionParameter[] parameters)
        : this(new MtprExceptionCode(code, message), null, parameters)
    {
    }

    public MtprException(string code,
        string message,
        Exception? innerException,
        params ExceptionParameter[] parameters)
        : this(new MtprExceptionCode(code, message), innerException, parameters)
    {
    }

    private static string buildMessage(MtprExceptionCode mtprExceptionCode,
        ExceptionParameter?[]? parameters)
    {
        ArgumentNullException.ThrowIfNull(mtprExceptionCode);

        parameters ??= [];

        if (parameters.Any(p => p is null))
            throw new ArgumentException("Parameters cannot contain null.", nameof(parameters));

        try
        {
            var text = string.Format(mtprExceptionCode.DisplayName,
                parameters.Select(p => p?.Value).ToArray());

            return string.IsNullOrWhiteSpace(mtprExceptionCode.Value)
                ? text
                : $"Error code({mtprExceptionCode.Value}): {text}";
        }
        catch (FormatException exception)
        {
            throw new FormatException("MtprException has not enough parameter.", 
                exception);
        }
    }
}
