using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Statements;

public class FuncDeclStatement : SyntaxStatement
{
    public Token NameToken { get; }

    public bool IsExtern { get; }

    public FunctionSymbol Symbol { get; set; } = null!;

    public MarshalType BoundReturnType { get; set; } = null!;
    public SyntaxTypeNode SyntaxReturnType { get; set; } = null!;

    public List<FuncParamDeclNode> Params { get; } 

    public ScopeStatement? Body { get; }

    public FuncDeclStatement(Token nameToken, SyntaxTypeNode syntaxReturnType, List<FuncParamDeclNode> parameters, ScopeStatement? body, bool isExtern)
    {
        NameToken = nameToken;
        SyntaxReturnType = syntaxReturnType;
        IsExtern = isExtern;
        Params = parameters;
        Body = body;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}