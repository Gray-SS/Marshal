namespace Marshal.Compiler.Errors;

public enum ErrorType : byte
{
    Warning,
    Error,
    Fatal,
    SyntaxError,
    InternalError,
    SemanticError,
}