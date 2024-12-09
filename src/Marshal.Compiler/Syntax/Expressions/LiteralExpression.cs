namespace Marshal.Compiler.Syntax.Expressions;

public enum LiteralType
{
    None,
    Int,
    String,
    Boolean,
}

public class LiteralExpression : SyntaxExpression
{
    public Token Token { get; }
    public LiteralType LiteralType { get; }

    public LiteralExpression(Token token, LiteralType literalType)
    {
        Token = token;
        LiteralType = literalType;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}