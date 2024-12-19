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
            TokenType.CharLiteral => LiteralType.Char,
            TokenType.FalseKeyword => LiteralType.Boolean,
            TokenType.TrueKeyword => LiteralType.Boolean,

            _ => LiteralType.None,
        };
    }

    public static bool IsNumericOperation(this UnaryOpType operation)
    {
        return operation switch
        {
            UnaryOpType.Negation => true,
            _ => false
        };
    }

    public static bool IsComparisonOperation(this UnaryOpType operation)
    {
        return operation switch
        {
            UnaryOpType.Not => true,
            _ => false
        };
    }

    public static bool IsNumericOperation(this BinOpType operation)
    {
        return operation switch
        {
            BinOpType.Addition => true,
            BinOpType.Division => true,
            BinOpType.Multiplication => true,
            BinOpType.Subtraction => true,
            BinOpType.Modulo => true,

            _ => false
        };
    }

    public static bool IsComparisonOperation(this BinOpType operation)
    {
        return operation switch
        {
            BinOpType.LessThan => true,
            BinOpType.LessThanEq => true,
            BinOpType.BiggerThan => true,
            BinOpType.BiggerThanEq => true,
            BinOpType.Equals => true,
            BinOpType.NotEquals => true,

            _ => false
        };
    }
}