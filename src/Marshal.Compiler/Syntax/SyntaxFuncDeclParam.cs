using Marshal.Compiler.Types;

namespace Marshal.Compiler.Syntax;

public class SyntaxFuncDeclParam
{
    public Token NameToken { get; }

    public MarshalType Type { get; }

    public SyntaxFuncDeclParam(MarshalType syntaxType, Token nameIdentifier)
    {
        Type = syntaxType;
        NameToken = nameIdentifier;
    }
}