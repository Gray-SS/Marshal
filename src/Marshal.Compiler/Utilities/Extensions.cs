using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Expressions;

namespace Marshal.Compiler.Utilities;

public static class Extensions
{
    public static LiteralType GetLiteralType(this Token token)
    {
        return token.Type switch
        {
            TokenType.StringLiteral => LiteralType.String,
            TokenType.IntLiteral => LiteralType.Int,

            _ => LiteralType.None,
        };
    }
}