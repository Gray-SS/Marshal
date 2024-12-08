using Marshal.Compiler.Syntax;
using Marshal.Compiler.Utilities;

namespace Marshal.Compiler.Errors;

public class ErrorHandler
{
    public bool HasError { get; private set; }

    private readonly Dictionary<ErrorType, (ConsoleColor color, string label, bool isError)> _errorConfig = new()
    {
        { ErrorType.Warning,        (ConsoleColor.Yellow,    "avertissement",    false) },
        { ErrorType.SyntaxError,    (ConsoleColor.Red,       "erreur de syntaxe", true) },
        { ErrorType.SemanticError,  (ConsoleColor.Red,       "erreur de sémantique", true) },
        { ErrorType.Error,          (ConsoleColor.Red,       "erreur",           true) },
        { ErrorType.Fatal,          (ConsoleColor.DarkRed,   "erreur fatale",    true) },
        { ErrorType.InternalError,  (ConsoleColor.DarkRed,   "erreur interne",   true) }
    };

    public void Report(ErrorType type, string message)
    {
        if (_errorConfig.TryGetValue(type, out var config))
        {
            if (config.isError) HasError = true;
            DisplayError(config.color, config.label, message);
        }
    }

    public void ReportDetailed(ErrorType type, string message, Location loc)
    {
        if (_errorConfig.TryGetValue(type, out var config))
        {
            if (config.isError) HasError = true;
            DisplayDetailedError(loc, config.color, config.label, message);
        }
    }

    private static void DisplayError(ConsoleColor color, string label, string message)
    {
        ConsoleHelper.Write(color, $"{label}: ");
        ConsoleHelper.WriteLine(ConsoleColor.Gray, message);
    }

    private static void DisplayDetailedError(Location loc, ConsoleColor color, string label, string message)
    {
        ConsoleHelper.Write(ConsoleColor.Magenta, $"{loc.RelativePath}:");
        ConsoleHelper.Write(color, $"{label}:{loc.Line}:{loc.Column}: ");
        ConsoleHelper.WriteLine(ConsoleColor.Gray, message);
    }
}