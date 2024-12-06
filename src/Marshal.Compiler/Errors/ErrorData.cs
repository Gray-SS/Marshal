namespace Marshal.Compiler.Errors;

public readonly struct ErrorData
{
    public ErrorType Type { get; }

    public string Message { get; }

    public ErrorData(ErrorType type, string message)
    {
        Type = type;
        Message = message;
    }
}