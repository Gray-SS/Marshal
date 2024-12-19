namespace Marshal.Compiler.Syntax.Expressions;

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

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}