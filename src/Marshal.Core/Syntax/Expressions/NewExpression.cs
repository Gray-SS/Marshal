using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Expressions;

public abstract class NewExpression : SyntaxExpression
{
    public Token TypeName { get; }

    public NewExpression(Location loc, Token typeName) : base(loc)
    {
        TypeName = typeName;
    }

    public override void Accept(IExprVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class NewStructExpression : NewExpression
{
    public List<SyntaxExpression> Arguments { get; }

    public override ValueCategory ValueCategory => ValueCategory.Transient;

    public NewStructExpression(Location loc, Token typeName, List<SyntaxExpression> arguments) : base(loc, typeName)
    {
        Arguments = arguments;
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        return Arguments;
    }
}

public class NewArrayExpression : NewExpression
{
    public SyntaxExpression LengthExpr { get; }
    public override ValueCategory ValueCategory => ValueCategory.Transient;

    public NewArrayExpression(Location loc, Token typeName, SyntaxExpression lengthExpr) : base(loc, typeName)
    {
        LengthExpr = lengthExpr;
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        yield return LengthExpr;
    }
}