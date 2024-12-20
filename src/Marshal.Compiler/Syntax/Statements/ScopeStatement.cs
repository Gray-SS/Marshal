namespace Marshal.Compiler.Syntax.Statements;

public class ScopeStatement : SyntaxStatement
{
    public List<SyntaxStatement> Statements { get; }
    public bool IsReturning => Statements.Any(x => x is ReturnStatement);

    public ScopeStatement(Location loc, List<SyntaxStatement> statements) : base(loc)
    {
        Statements = statements;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(ScopeStatement)}]", level);
        foreach (SyntaxStatement stmt in Statements)
        {
            stmt.Dump(level + 1);
        }
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}