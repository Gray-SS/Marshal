using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Statements;

public class ScopeStatement : SyntaxStatement
{
    public List<SyntaxStatement> Statements { get; }
    public bool IsReturning => Statements.Any(x => x is ReturnStatement);

    public ScopeStatement(Location loc, List<SyntaxStatement> statements) : base(loc)
    {
        Statements = statements;
    }

    public override void Accept(IStmtVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        return Statements;
    }
}