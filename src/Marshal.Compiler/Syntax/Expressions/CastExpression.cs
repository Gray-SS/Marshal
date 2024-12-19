namespace Marshal.Compiler.Syntax.Expressions;

public sealed class CastExpression : SyntaxExpression
{
    public SyntaxTypeNode CastedType { get; }
    public SyntaxExpression CastedExpr { get; }

    public override ValueCategory ValueCategory => ValueCategory.Transient;

    public CastExpression(Location loc, SyntaxTypeNode castedType, SyntaxExpression castedExpr) : base(loc)
    {
        CastedType = castedType;
        CastedExpr = castedExpr;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}