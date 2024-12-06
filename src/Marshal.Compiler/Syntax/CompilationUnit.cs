namespace Marshal.Compiler.Syntax;

public class CompilationUnit : SyntaxNode
{
    public List<SyntaxStatement> Statements { get; }

    public CompilationUnit(List<SyntaxStatement> statements)
    {
        Statements = statements;
    }
}