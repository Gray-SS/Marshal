namespace Marshal.Compiler.Syntax;

public class CompilationUnit : SyntaxNode
{
    public List<SyntaxStatement> Statements { get; }

    public CompilationUnit(List<SyntaxStatement> statements)
    {
        Statements = statements;
    }

    public override void Accept(IVisitor visitor)
    {
        visitor.Visit(this);
    }
}