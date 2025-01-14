using Marshal.Core.Semantics;
using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Statements;

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

    public override void Accept(IStmtVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        foreach (var param in Params)
            yield return param;

        if (Body != null)
            yield return Body;
    }
}