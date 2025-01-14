using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Expressions;

public enum LiteralType
{
    None,
    Int,
    String,
    Boolean,
    Char,
}

public class LiteralExpression : SyntaxExpression
{
    public Token Token { get; }
    public LiteralType LiteralType { get; }

    public override ValueCategory ValueCategory => ValueCategory.Transient;

    public LiteralExpression(Location loc, Token token, LiteralType literalType) : base(loc)
    {
        Token = token;
        LiteralType = literalType;
    }

    public override void Accept(IExprVisitor visitor)
    {
        visitor.Visit(this);
    }
}