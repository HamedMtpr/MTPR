namespace Mtpr.Core.Exceptions;

public interface IMtprException : IException
{
    IList<ExceptionParameter>? Parameters { get; }
}