namespace Marshal.Compiler.Syntax;

public class CompilationUnit : SyntaxNode
{
    public List<SyntaxStatement> Statements { get; }

    public CompilationUnit(List<SyntaxStatement> statements) : base(default)
    {
        Statements = statements;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(CompilationUnit)}]", level);
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