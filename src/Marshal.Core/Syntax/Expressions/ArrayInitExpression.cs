using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Expressions;

public class ArrayInitExpression : SyntaxExpression
{
    public List<SyntaxExpression> Expressions { get; }
    public override ValueCategory ValueCategory => ValueCategory.Transient;

    public ArrayInitExpression(Location loc, List<SyntaxExpression> expressions) : base(loc)
    {
        Expressions = expressions;
    }

    public override void Accept(IExprVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        return Expressions;
    }
}