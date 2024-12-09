using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax;

public abstract class SyntaxExpression : SyntaxNode
{
    /// <summary>
    /// This type is set in the Type Checker compiler pass and <b>MUST not be used before</b> this stage
    /// </summary>
    public MarshalType Type { get; set; } = null!;
}