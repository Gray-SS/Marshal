namespace Marshal.Compiler.Syntax;

public class CompilationUnit : SyntaxNode
{
    public List<SyntaxStatement> Statements { get; }

    public CompilationUnit(List<SyntaxStatement> statements) : base(default)
    {
        Statements = statements;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}