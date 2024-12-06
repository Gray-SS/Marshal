namespace Marshal.Compiler.Syntax.Statements;

public class ScopeStatement : SyntaxStatement
{
    public List<SyntaxStatement> Statements { get; }

    public ScopeStatement(List<SyntaxStatement> statements)
    {
        Statements = statements;
    }
}