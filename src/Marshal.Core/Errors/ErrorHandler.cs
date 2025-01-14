using Marshal.Core.Syntax;

namespace Marshal.Core.Errors;

public class ErrorHandler
{
    public bool HasError { get; private set; }
    private readonly IErrorLogger _logger;

    public ErrorHandler(IErrorLogger logger)
    {
        _logger = logger;
    }

    public void Report(ErrorType type, string message)
    {
        if (IsError(type)) HasError = true;

        _logger.LogError(type, message);
    }

    public void ReportDetailed(Location location, ErrorType type, string message)
    {
        if (IsError(type)) HasError = true;

        _logger.LogError(location, type, message);
    }

    private static bool IsError(ErrorType type)
    {
        return type switch 
        {
            > ErrorType.Warning => true,
            _ => false
        };
    }
}