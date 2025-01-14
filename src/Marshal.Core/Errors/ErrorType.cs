/// <summary>
/// Represents the different types of errors that can occur within the Marshal.Core.Errors namespace. <br/> <br/>
/// <b>Note for devs:</b> The logic related to this enum is in the <see cref="ErrorHandler"/> class. Make sure to 
/// check this class if changes are made here 
/// </summary>
public enum ErrorType : byte
{
    /// <summary>
    /// Indicates a warning that does not prevent the program from running.
    /// </summary>
    Warning,

    /// <summary>
    /// Indicates a recoverable error that may affect program execution.
    /// </summary>
    Error,

    /// <summary>
    /// Indicates a critical error that causes the program to terminate.
    /// </summary>
    Fatal,

    /// <summary>
    /// Indicates an error related to incorrect syntax in the code.
    /// </summary>
    SyntaxError,

    /// <summary>
    /// Indicates an error that occurs internally within the system.
    /// </summary>
    InternalError,

    /// <summary>
    /// Indicates an error related to the meaning or logic of the code.
    /// </summary>
    SemanticError,
}