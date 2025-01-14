using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Statements;

public sealed class WhileStatement : SyntaxStatement
{
    public ScopeStatement Scope { get; }
    public SyntaxExpression CondExpr { get; }

    public WhileStatement(Location loc, SyntaxExpression condExpr, ScopeStatement scope) : base(loc)
    {
        Scope = scope;
        CondExpr = condExpr;
    }

    public override void Accept(IStmtVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        yield return CondExpr;
        yield return Scope;
    }
}