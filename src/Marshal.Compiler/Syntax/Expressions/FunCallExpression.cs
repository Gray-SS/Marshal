namespace Marshal.Compiler.Syntax.Expressions;

public class FunCallExpression : SyntaxExpression
{
    public Token NameIdentifier { get; }

    public List<SyntaxExpression> Parameters { get; }

    public FunCallExpression(Token nameIdentifier, List<SyntaxExpression> parameters)
    {
        Parameters = parameters;
        NameIdentifier = nameIdentifier;
    }
}