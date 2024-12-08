using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Statements;

public class AssignmentStatement : SyntaxStatement
{
    public Token NameToken { get; }

    public SyntaxExpression AssignExpr { get; }

    public VariableSymbol Symbol { get; set; } = null!;

    public AssignmentStatement(Token nameIdentifier, SyntaxExpression assignExpr)
    {
        NameToken = nameIdentifier;
        AssignExpr = assignExpr;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}