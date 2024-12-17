using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Expressions;

public abstract class NewExpression : SyntaxExpression
{
    public Token TypeName { get; }

    public NewExpression(Token typeName)
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

    public NewArrayExpression(Token typeName, SyntaxExpression lengthExpr) : base(typeName)
    {
        LengthExpr = lengthExpr;
    }
}