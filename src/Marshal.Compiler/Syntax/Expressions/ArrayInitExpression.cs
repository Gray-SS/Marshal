namespace Marshal.Compiler.Syntax.Expressions;

public class ArrayInitExpression : SyntaxExpression
{
    public List<SyntaxExpression> Expressions { get; }

    public ArrayInitExpression(List<SyntaxExpression> expressions)
    {
        Expressions = expressions;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}