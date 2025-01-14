using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Statements;

public class ConditionalScope : SyntaxNode
{
    public ScopeStatement Scope { get; }
    public SyntaxExpression ConditionExpr { get; }

    public ConditionalScope(ScopeStatement scope, SyntaxExpression condExpr) : base(condExpr.Loc)
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

    public override void Accept(IStmtVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        foreach (ConditionalScope child in IfsScopes)
            yield return child;

        if (ElseScope != null)
            yield return ElseScope; 
    }
}