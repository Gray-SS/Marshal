using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Expressions;

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

    public override void Accept(IExprVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        yield return CastedExpr;
    }
}