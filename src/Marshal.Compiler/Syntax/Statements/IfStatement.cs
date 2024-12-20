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

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(IfStatement)}]", level);
        foreach (ConditionalScope scope in IfsScopes)
        {
            Dump($"[{nameof(ConditionalScope)}]", level + 1);
            scope.ConditionExpr.Dump(level + 2);
            scope.Scope.Dump(level + 2);
        }

        ElseScope?.Dump(level + 1);
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}