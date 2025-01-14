using Marshal.Core.Errors;
using Marshal.Core.Syntax;
using Marshal.Core.Utils;

namespace Marshal.Compiler;

public class ErrorLogger : IErrorLogger
{
    public static readonly ErrorLogger Default = new();

    private readonly Dictionary<ErrorType, (ConsoleColor color, string label, bool isError)> _errorConfig = new()
    {
        { ErrorType.Warning,        (ConsoleColor.Yellow,    "avertissement",    false) },
        { ErrorType.SyntaxError,    (ConsoleColor.Red,       "erreur de syntaxe", true) },
        { ErrorType.SemanticError,  (ConsoleColor.Red,       "erreur de sémantique", true) },
        { ErrorType.Error,          (ConsoleColor.Red,       "erreur",           true) },
        { ErrorType.Fatal,          (ConsoleColor.DarkRed,   "erreur fatale",    true) },
        { ErrorType.InternalError,  (ConsoleColor.DarkRed,   "erreur interne",   true) }
    };

    public void LogError(ErrorType type, string message)
    {
        if (!_errorConfig.TryGetValue(type, out var config))
            throw new NotImplementedException($"the logging of error of type '{type}' isn't implemented");

        DisplayError(config.color, config.label, message);
    }

    public void LogError(Location loc, ErrorType type, string message)
    {
        if (!_errorConfig.TryGetValue(type, out var config))
            throw new NotImplementedException($"the logging of error of type '{type}' isn't implemented");

        DisplayDetailedError(loc, config.color, config.label, message);
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