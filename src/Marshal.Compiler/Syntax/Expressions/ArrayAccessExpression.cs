namespace Marshal.Compiler.Syntax.Expressions;

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

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}