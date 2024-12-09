using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax;

public class FuncParamDeclNode
{
    public Token NameToken { get; }

    public MarshalType BoundType { get; set; } = null!;
    public SyntaxTypeNode SyntaxType { get; }

    public FuncParamDeclNode(SyntaxTypeNode syntaxType, Token nameIdentifier)
    {
        SyntaxType = syntaxType;
        NameToken = nameIdentifier;
    }
}