namespace Marshal.Compiler.Syntax.Statements;

public class FuncDeclStatement : SyntaxStatement
{
    public Token NameToken { get; }

    public Token TypeToken { get; }

    public bool IsExtern { get; }

    public List<SyntaxFuncDeclParam> Params { get; } 

    public ScopeStatement? Body { get; }


    public FuncDeclStatement(Token nameToken, Token typeToken, List<SyntaxFuncDeclParam> parameters, ScopeStatement? body, bool isExtern)
    {
        NameToken = nameToken;
        TypeToken = typeToken;
        IsExtern = isExtern;
        Params = parameters;
        Body = body;
    }
}