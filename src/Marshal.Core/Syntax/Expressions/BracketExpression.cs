using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Expressions;

public sealed class BracketExpression : SyntaxExpression
{
    public SyntaxExpression Expression { get; }
    public override ValueCategory ValueCategory => Expression.ValueCategory;

    public BracketExpression(Location loc, SyntaxExpression expr) : base(loc)
    {  
        Expression = expr;
    }

    public override void Accept(IExprVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        yield return Expression;
    }
}
