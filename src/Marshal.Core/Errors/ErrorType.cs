namespace Marshal.Core.Errors;

public enum ErrorType : byte
{
    Warning,
    Error,
    Fatal,
    SyntaxError,
    InternalError,
    SemanticError,
}