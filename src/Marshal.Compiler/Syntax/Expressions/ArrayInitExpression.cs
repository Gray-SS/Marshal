namespace Marshal.Compiler.Syntax.Expressions;

public class ArrayInitExpression : SyntaxExpression
{
    public List<SyntaxExpression> Expressions { get; }
    public override ValueCategory ValueCategory => ValueCategory.Transient;

    public ArrayInitExpression(Location loc, List<SyntaxExpression> expressions) : base(loc)
    {
        Expressions = expressions;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}