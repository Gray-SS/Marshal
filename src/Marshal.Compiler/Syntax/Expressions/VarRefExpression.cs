namespace Marshal.Compiler.Syntax.Expressions;

public class VarRefExpression : SyntaxExpression
{
    public Token NameIdentifier { get; }

    public VarRefExpression(Token nameIdentifier)
    {
        NameIdentifier = nameIdentifier;
    }

    public override void Accept(IVisitor visitor)
    {
        visitor.Visit(this);
    }
}