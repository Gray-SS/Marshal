using Marshal.Compiler.Errors;
using Marshal.Compiler.Syntax;

namespace Marshal.Compiler;

public abstract class CompilerPass
{
    protected CompilationContext Context { get; }

    protected ErrorHandler ErrorHandler { get; }

    public CompilerPass(CompilationContext source, ErrorHandler errorHandler)
    {
        Context = source;
        ErrorHandler = errorHandler;
    }

    public abstract void Apply();

    protected void Report(ErrorType type, string message)
    {
        ErrorHandler.Report(type, message);
    }

    protected void ReportDetailed(ErrorType type, string message, Location loc)
    {
        ErrorHandler.ReportDetailed(type, message, loc);
    }
}