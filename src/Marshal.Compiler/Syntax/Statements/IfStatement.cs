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
    public ConditionalScope IfScope { get; }
    public List<ConditionalScope> ElseIfScopes { get; }
    public ScopeStatement? ElseScope { get; }

    public IfStatement(ConditionalScope ifScope, List<ConditionalScope> elseIfScopes, ScopeStatement? elseScope)
    {
        IfScope = ifScope;
        ElseIfScopes = elseIfScopes;
        ElseScope = elseScope;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}