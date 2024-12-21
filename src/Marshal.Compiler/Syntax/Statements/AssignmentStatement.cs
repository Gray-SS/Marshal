namespace Marshal.Compiler.Syntax.Statements;

public class AssignmentStatement : SyntaxStatement
{
    public SyntaxExpression LExpr { get; }

    public SyntaxExpression Initializer { get; }

    public AssignmentStatement(Location loc, SyntaxExpression lExpr, SyntaxExpression assignExpr) : base(loc)
    {
        LExpr = lExpr;
        Initializer = assignExpr;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(AssignmentStatement)}]", level);
        LExpr.Dump(level + 1);
        Initializer.Dump(level + 1);
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}