using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Statements;

public class AssignmentStatement : SyntaxStatement
{
    public SyntaxExpression LExpr { get; }

    public SyntaxExpression Initializer { get; }

    public AssignmentStatement(Location loc, SyntaxExpression lExpr, SyntaxExpression assignExpr) : base(loc)
    {
        LExpr = lExpr;
        Initializer = assignExpr;
    }

    public override void Accept(IStmtVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        yield return LExpr;
        yield return Initializer;
    }
}