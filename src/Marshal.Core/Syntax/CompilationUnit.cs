using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax;

public class CompilationUnit : SyntaxNode<IUnitVisitor>
{
    public List<SyntaxStatement> Statements { get; }

    public CompilationUnit(List<SyntaxStatement> statements) : base(default)
    {
        Statements = statements;
    }

    public override void Accept(IUnitVisitor visitor)
    {
        visitor.Visit(this);
    }
}