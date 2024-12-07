namespace Marshal.Compiler.Syntax.Statements;

public class AssignmentStatement : SyntaxStatement
{
    public Token NameIdentifier { get; }

    public SyntaxExpression AssignExpr { get; }

    public AssignmentStatement(Token nameIdentifier, SyntaxExpression assignExpr)
    {
        NameIdentifier = nameIdentifier;
        AssignExpr = assignExpr;
    }

    public override void Accept(IVisitor visitor)
    {
        visitor.Visit(this);
    }
}