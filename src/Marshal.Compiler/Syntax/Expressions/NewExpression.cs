namespace Marshal.Compiler.Syntax.Expressions;

public abstract class NewExpression : SyntaxExpression
{
    public Token TypeName { get; }

    public NewExpression(Location loc, Token typeName) : base(loc)
    {
        TypeName = typeName;
    } 

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
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
}