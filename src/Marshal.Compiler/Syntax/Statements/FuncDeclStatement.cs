using Marshal.Compiler.Semantics;
using Marshal.Compiler.Types;

namespace Marshal.Compiler.Syntax.Statements;

public class FuncDeclStatement : SyntaxStatement
{
    public Token NameToken { get; }

    public bool IsExtern { get; }

    public FunctionSymbol Symbol { get; set; } = null!;

    public MarshalType ReturnType { get; set; } = null!;

    public List<SyntaxFuncDeclParam> Params { get; } 

    public ScopeStatement? Body { get; }

    public FuncDeclStatement(Token nameToken, MarshalType returnType, List<SyntaxFuncDeclParam> parameters, ScopeStatement? body, bool isExtern)
    {
        NameToken = nameToken;
        ReturnType = returnType;
        IsExtern = isExtern;
        Params = parameters;
        Body = body;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}