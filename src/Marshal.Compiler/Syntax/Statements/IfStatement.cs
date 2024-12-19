namespace Marshal.Compiler.Syntax.Statements;

public class ConditionalScope
{
    public ScopeStatement Scope { get; }
    public SyntaxExpression ConditionExpr { get; }

    public ConditionalScope(ScopeStatement scope, SyntaxExpression condExpr)
    {
        Scope = scope;
        ConditionExpr = condExpr;
    }
}

public sealed class IfStatement : SyntaxStatement
{
    public List<ConditionalScope> IfsScopes { get; }
    public ScopeStatement? ElseScope { get; }

    public IfStatement(Location loc, List<ConditionalScope> ifScopes, ScopeStatement? elseScope) : base(loc)
    {
        IfsScopes = ifScopes;
        ElseScope = elseScope;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}