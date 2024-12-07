namespace Marshal.Compiler.Syntax.Expressions;

public class LiteralExpression : SyntaxExpression
{
    public Token LiteralToken { get; }

    public LiteralExpression(Token literalToken)
    {
        LiteralToken = literalToken;
    }

    public override void Accept(IVisitor visitor)
    {
        visitor.Visit(this);
    }
}