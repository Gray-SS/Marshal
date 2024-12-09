using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Statements;

public class AssignmentStatement : SyntaxStatement
{
    public Token NameToken { get; }

    public SyntaxExpression Initializer { get; }

    public VariableSymbol Symbol { get; set; } = null!;

    public AssignmentStatement(Token nameIdentifier, SyntaxExpression assignExpr)
    {
        NameToken = nameIdentifier;
        Initializer = assignExpr;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}