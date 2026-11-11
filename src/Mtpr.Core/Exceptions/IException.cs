namespace Mtpr.Core.Exceptions;

public interface IException
{
    string Code { get; }
    string Message { get; }
}
