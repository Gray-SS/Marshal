namespace Marshal.Compiler.Syntax.Expressions;

public sealed class BracketExpression : SyntaxExpression
{
    public SyntaxExpression Expression { get; }
    public override ValueCategory ValueCategory => Expression.ValueCategory;

    public BracketExpression(Location loc, SyntaxExpression expr) : base(loc)
    {  
        Expression = expr;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(BracketExpression)}]", level);
        Expression.Dump(level + 1);
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}
