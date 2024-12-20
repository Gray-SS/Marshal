namespace Marshal.Compiler.Syntax.Statements;

public sealed class WhileStatement : SyntaxStatement
{
    public ScopeStatement Scope { get; }
    public SyntaxExpression CondExpr { get; }

    public WhileStatement(Location loc, SyntaxExpression condExpr, ScopeStatement scope) : base(loc)
    {
        Scope = scope;
        CondExpr = condExpr;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(WhileStatement)}]", level);
        CondExpr.Dump(level + 1);
        Scope.Dump(level + 1);
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}