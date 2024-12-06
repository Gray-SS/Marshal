using Marshal.Compiler.Errors;
using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Expressions;

namespace Marshal.Compiler.Semantics;

public class TypeEvaluator
{
    private readonly SymbolTable _symbols;
    private readonly ErrorHandler _errorHandler;

    public TypeEvaluator(SymbolTable symbols, ErrorHandler errorHandler)
    {
        _symbols = symbols;
        _errorHandler = errorHandler;
    }

    public TypeSymbol Evaluate(SyntaxExpression expr)
    {
        switch (expr)
        {
            case LiteralExpression literalExpr:
                return EvaluateTypeLiteralExpression(literalExpr);
            case VarRefExpression varRefExpr:
                return EvaluateVarRefExpression(varRefExpr);
            case FunCallExpression funCallExpr:
                return EvaluateFunCallExpression(funCallExpr);
            default:
                _errorHandler.Report(ErrorType.InternalError, $"l'évaluation du type '{expr.GetType().Name}' n'est pas implémentée.");
                return Symbol.Void;
        }
    }

    private TypeSymbol EvaluateTypeLiteralExpression(LiteralExpression literal)
    {
        var token = literal.LiteralToken;
        
        if (token.Type == TokenType.NumberLiteral) 
            return Symbol.Int;

        return Symbol.Void;
    }

    private TypeSymbol EvaluateVarRefExpression(VarRefExpression expr)
    {
        var variable = _symbols.GetSymbol(expr.NameIdentifier.Value, SymbolType.Variable) as VariableSymbol;
        if (variable == null)
            return Symbol.Void;

        return variable.VariableType;
    }

    private TypeSymbol EvaluateFunCallExpression(FunCallExpression expr)
    {
        var function = _symbols.GetSymbol(expr.NameIdentifier.Value, SymbolType.Function) as FunctionSymbol;
        if (function == null)
            return Symbol.Void;

        return function.ReturnType;
    }
}