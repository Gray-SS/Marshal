using Marshal.Core.Errors;
using Marshal.Core.Syntax;

namespace Marshal.Core;

public abstract class CompilerPass
{
    protected CompilationContext Context { get; }

    protected ErrorHandler ErrorHandler { get; }

    public CompilerPass(CompilationContext context, ErrorHandler errorHandler)
    {
        Context = context;
        ErrorHandler = errorHandler;
    }
    
    protected void Report(ErrorType type, string message)
    {
        ErrorHandler.Report(type, message);
    }

    protected void ReportDetailed(ErrorType type, string message, Location loc)
    {
        ErrorHandler.ReportDetailed(type, message, loc);
    }
}