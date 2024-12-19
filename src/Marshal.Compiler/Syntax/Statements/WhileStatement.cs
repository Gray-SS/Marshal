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

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}