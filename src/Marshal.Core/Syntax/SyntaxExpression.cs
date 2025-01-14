using Marshal.Core.Semantics;
using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax;

public enum ValueCategory 
{
    /// <summary>
    /// Stands for LValue in other standards language. It represents a memory address.
    /// </summary>
    Locator,

    /// <summary>
    /// Stands for RValue in other standards language. It represents a temporary, computed value.
    /// </summary>
    Transient
}

public abstract class SyntaxExpression : SyntaxNode<IExprVisitor>
{
    /// <summary>
    /// Represents the category of the evaluated expression.
    /// </summary>
    public abstract ValueCategory ValueCategory { get; }

    /// <summary>
    /// This type is set in the Type Checker compiler pass and <b>MUST not be used before</b> this stage
    /// </summary>
    public MarshalType Type { get; set; } = null!;

    protected SyntaxExpression(Location loc) : base(loc)
    {
    }
}