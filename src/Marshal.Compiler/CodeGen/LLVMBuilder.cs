using System.Text;
using Marshal.Compiler.Semantics;
using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;

namespace Marshal.Compiler.CodeGen;

public class LLVMBuilder
{
    private int _tempCount = 0;
    private int _currentScope;
    private readonly StringBuilder _sb;
    private readonly TypeEvaluator _typeEvaluator;

    public LLVMBuilder(TypeEvaluator typeEvaluator)
    {
        _sb = new StringBuilder();
        _typeEvaluator = typeEvaluator;
    }

    public void Clear()
    {
        _sb.Clear();
    }

    public void AddFunctionSignature(FunctionSymbol symbol, FuncDeclStatement statement)
    {
        var parameters = string.Join(", ", symbol.Params.Select(p => {
            if (p.IsParams)
                return "...";
            
            return $"{ToLLVMType(p.Type)} %{p.Name}";
        }));

        string funcType = statement.Body != null ? "define" : "declare";

        Append($"{funcType} {ToLLVMType(symbol.ReturnType)} @{symbol.Name}({parameters})");
    }

    public void BeginScope()
    {
        Append("{");
        _currentScope++;
    }
    
    public void EndScope()
    {
        _currentScope--;
        Append("}");
    }

    public void AllocVariable(VariableSymbol var)
    {
        string type = ToLLVMType(var.VariableType);
        Append($"%{var.Name} = alloca {type}");
    }

    public void StoreValue(VariableSymbol var, SyntaxExpression expr)
    {
        string type = ToLLVMType(var.VariableType);
        string value = EvaluateExpr(expr);
        Append($"store {type} {value}, {type}* %{var.Name}");
    }

    public void ReturnValue(SyntaxExpression expr)
    {
        var exprType = _typeEvaluator.Evaluate(expr);
        string type = ToLLVMType(exprType);
        string value = EvaluateExpr(expr);
        Append($"ret {type} {value}");
    }

    public void CallFunction(FunctionSymbol symbol, List<SyntaxExpression> parameters)
    {
        string llvmType = ToLLVMType(symbol.ReturnType);

        var strParams = new string[parameters.Count];
        for (int i = 0; i < parameters.Count; i++)
        {
            strParams[i] = $"{ToLLVMType(symbol.Params[i].Type)} {EvaluateExpr(parameters[i])}";
        }

        Append($"call {llvmType} @{symbol.Name}({string.Join(',', strParams)})");
    }

    private string EvaluateExpr(SyntaxExpression expr)
    {
        var exprType = _typeEvaluator.Evaluate(expr);

        switch (expr)
        {
            case LiteralExpression literalExpr:
                return literalExpr.LiteralToken.Value;

            case VarRefExpression varRefExpr:
            {
                string varTemp = GetTempVariable();
                string llvmType = ToLLVMType(exprType);

                Append($"{varTemp} = load {llvmType}, {llvmType}* %{varRefExpr.NameIdentifier.Value}");
                return varTemp;
            }

            case FunCallExpression funCallExpr:
            {
                string varTemp = GetTempVariable();
                string llvmType = ToLLVMType(exprType);

                var strParams = funCallExpr.Parameters.Select(param => $"{ToLLVMType(_typeEvaluator.Evaluate(param))} {EvaluateExpr(param)}");

                Append($"{varTemp} = call {llvmType} @{funCallExpr.NameIdentifier.Value}({string.Join(',', strParams)})");
                return varTemp;
            }

            default:
                throw new NotImplementedException();
        }
    }

    private string GetTempVariable()
    {
        return $"%temp{_tempCount++}";
    }
    
    private void WriteScope()
    {
        _sb.Append(' ', 4 * _currentScope);
    }

    private void Append(string text)
    {
        WriteScope();
        _sb.AppendLine(text);
    }

    public string Build()
    {
        return _sb.ToString();
    }

    private static string ToLLVMType(TypeSymbol type)
    {
        if (type.IsPrimitive)
            return type.LLVMType!;

        throw new NotImplementedException("custom type isn't supported.");
    }
}