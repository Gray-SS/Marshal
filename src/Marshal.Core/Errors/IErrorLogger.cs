using Marshal.Core.Syntax;

namespace Marshal.Core.Errors;

public interface IErrorLogger
{
    void LogError(ErrorType type, string message);
    void LogError(Location loc, ErrorType type, string message);
}