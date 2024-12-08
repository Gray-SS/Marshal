namespace Marshal.Compiler.Syntax.Expressions;

public enum LiteralType
{
    None,
    Int,
    String,
}

public class LiteralExpression : SyntaxExpression
{
    public LiteralType LiteralType { get; }

    public LiteralExpression(LiteralType literalType)
    {
        LiteralType = literalType;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}