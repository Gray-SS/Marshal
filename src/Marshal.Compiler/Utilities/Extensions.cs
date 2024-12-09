using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;

namespace Marshal.Compiler.Utilities;

public static class Extensions
{
    public static LiteralType GetLiteralType(this Token token)
    {
        return token.Type switch
        {
            TokenType.StringLiteral => LiteralType.String,
            TokenType.IntLiteral => LiteralType.Int,
            TokenType.FalseKeyword => LiteralType.Boolean,
            TokenType.TrueKeyword => LiteralType.Boolean,

            _ => LiteralType.None,
        };
    }
    public static bool IsNumericBinOpType(this BinOperatorType binOpType)
    {
        return binOpType switch
        {
            BinOperatorType.Addition => true,
            BinOperatorType.Division => true,
            BinOperatorType.Multiplication => true,
            BinOperatorType.Subtraction => true,

            _ => false,
        };
    }
}