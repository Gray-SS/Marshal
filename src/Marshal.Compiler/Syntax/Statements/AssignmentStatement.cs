using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Statements;

public class AssignmentStatement : SyntaxStatement
{
    public Token NameToken { get; }

    public SyntaxExpression LExpr { get; }

    public SyntaxExpression Initializer { get; }

    public VariableSymbol Symbol { get; set; } = null!;

    public AssignmentStatement(Location loc, Token nameToken, SyntaxExpression lExpr, SyntaxExpression assignExpr) : base(loc)
    {
        NameToken = nameToken;
        LExpr = lExpr;
        Initializer = assignExpr;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(AssignmentStatement)}:{NameToken.Value}]", level);
        LExpr.Dump(level + 1);
        Initializer.Dump(level + 1);
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}