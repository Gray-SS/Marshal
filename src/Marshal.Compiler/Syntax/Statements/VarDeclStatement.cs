namespace Marshal.Compiler.Syntax.Statements;

public class VarDeclStatement : SyntaxStatement
{
    public Token NameIdentifier { get; }

    public Token TypeIdentifier { get; }

    public SyntaxExpression? InitExpression { get; }

    public VarDeclStatement(Token nameIdentifier, Token typeIdentifier, SyntaxExpression? initExpression)
    {
        NameIdentifier = nameIdentifier;
        TypeIdentifier = typeIdentifier;
        InitExpression = initExpression;
    }
}