using Marshal.Core.Semantics;

namespace Marshal.Core.Syntax;

public class FuncParamDeclNode : SyntaxNode
{
    public Token NameToken { get; }

    public MarshalType BoundType { get; set; } = null!;
    public SyntaxTypeNode SyntaxType { get; }

    public FuncParamDeclNode(SyntaxTypeNode syntaxType, Token nameIdentifier) : base(nameIdentifier.Loc)
    {
        SyntaxType = syntaxType;
        NameToken = nameIdentifier;
    }
}