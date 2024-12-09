namespace Marshal.Compiler.Syntax.Expressions;

public class ArrayAccessExpression : SyntaxExpression
{
    public SyntaxExpression ArrayExpr { get; }

    public SyntaxExpression IndexExpr { get; }

    public ArrayAccessExpression(SyntaxExpression arrayExpr, SyntaxExpression indexExpr)
    {
        ArrayExpr = arrayExpr;
        IndexExpr = indexExpr;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}