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

    public FuncDeclStatement(Location loc, Token nameToken, SyntaxTypeNode syntaxReturnType, List<FuncParamDeclNode> parameters, ScopeStatement? body, bool isExtern) : base(loc)
    {
        NameToken = nameToken;
        SyntaxReturnType = syntaxReturnType;
        IsExtern = isExtern;
        Params = parameters;
        Body = body;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(FuncDeclStatement)}:{NameToken.Value}:({string.Join(',', Params.Select(x => x.NameToken.Value))})]: {SyntaxReturnType.Name}", level);
        Body?.Dump(level + 1);
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}