namespace Marshal.Compiler.Syntax.Statements;

public class FunCallStatement : SyntaxStatement
{
    public Token NameIdentifier { get; }

    public List<SyntaxExpression> Parameters { get; }

    public FunCallStatement(Token nameIdentifier, List<SyntaxExpression> parameters)
    {
        Parameters = parameters;
        NameIdentifier = nameIdentifier;
    }
}