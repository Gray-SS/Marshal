namespace Marshal.Compiler.Syntax;

public class SyntaxFuncDeclParam
{
    public Token TypeIdentifier { get; }

    public Token NameIdentifier { get; }

    public Token? ParamsToken { get; }

    public bool IsParams => ParamsToken != null;

    public SyntaxFuncDeclParam(Token paramsToken)
    {
        TypeIdentifier = null!;
        NameIdentifier = null!;
        ParamsToken = paramsToken;
    }

    public SyntaxFuncDeclParam(Token typeIdentifier, Token nameIdentifier)
    {
        TypeIdentifier = typeIdentifier;
        NameIdentifier = nameIdentifier;
    }
}