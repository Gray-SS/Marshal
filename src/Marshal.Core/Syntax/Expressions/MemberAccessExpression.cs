using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Expressions;

public class MemberAccessExpression : SyntaxExpression
{
    public string MemberName { get; }
    public SyntaxExpression VarExpr { get; }

    public int MemberIdx { get; set; }

    public override ValueCategory ValueCategory => ValueCategory.Locator;

    public MemberAccessExpression(Location loc, SyntaxExpression varExpr, string accessorName) : base(loc)
    {
        VarExpr = varExpr;
        MemberName = accessorName;
    }

    public override void Accept(IExprVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        yield return VarExpr;
    }
}