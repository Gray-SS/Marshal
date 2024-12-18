namespace Marshal.Compiler.Syntax.Statements;

public sealed class WhileStatement : SyntaxStatement
{
    public ScopeStatement Scope { get; }
    public SyntaxExpression CondExpr { get; }

    public WhileStatement(SyntaxExpression condExpr, ScopeStatement scope)
    {
        Scope = scope;
        CondExpr = condExpr;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}