using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Expressions;

public class FunCallExpression : SyntaxExpression
{
    public Token NameToken { get; }

    public List<SyntaxExpression> Parameters { get; }

    public FunctionSymbol Symbol { get; set; } = null!;

    public FunCallExpression(Token nameIdentifier, List<SyntaxExpression> parameters)
    {
        Parameters = parameters;
        NameToken = nameIdentifier;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}