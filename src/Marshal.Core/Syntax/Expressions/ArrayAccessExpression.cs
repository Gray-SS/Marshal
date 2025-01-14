using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Expressions;

public class ArrayAccessExpression : SyntaxExpression
{
    public SyntaxExpression ArrayExpr { get; }

    public SyntaxExpression IndexExpr { get; }

    public override ValueCategory ValueCategory => ValueCategory.Locator;

    public ArrayAccessExpression(Location loc, SyntaxExpression arrayExpr, SyntaxExpression indexExpr) : base(loc)
    {
        ArrayExpr = arrayExpr;
        IndexExpr = indexExpr;
    }

    public override void Accept(IExprVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        yield return ArrayExpr;
        yield return IndexExpr;
    }
}