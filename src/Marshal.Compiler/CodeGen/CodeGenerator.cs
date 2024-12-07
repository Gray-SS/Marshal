using Swigged.LLVM;
using Marshal.Compiler.Syntax;
using Marshal.Compiler.Semantics;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;

namespace Marshal.Compiler.CodeGen;

public class CodeGenerator : IVisitor
{
    private int _globalStringCount;
    private readonly ModuleRef _module;
    private readonly BuilderRef _builder;

    private readonly Stack<ValueRef> _valueStack;
    private readonly Dictionary<string, ValueRef> _namedValues;

    public CodeGenerator(ModuleRef module)
    {
        _valueStack = new Stack<ValueRef>();
        _namedValues = new Dictionary<string, ValueRef>();

        _module = module;
        _builder = LLVM.CreateBuilder();
    }

    public void Visit(CompilationUnit unit)
    {
        foreach (var statement in unit.Statements)
        {
            statement.Accept(this);
        }
    }

    public void Visit(AssignmentStatement stmt)
    {
        string varName = stmt.NameIdentifier.Value;
        if (!_namedValues.TryGetValue(varName, out ValueRef varPtr))
            throw new Exception("variable is not declared");

        stmt.AssignExpr.Accept(this);
        LLVM.BuildStore(_builder, _valueStack.Pop(), varPtr);
    }

    public void Visit(ScopeStatement stmt)
    {
        foreach (var statement in stmt.Statements)
        {
            statement.Accept(this);
        }
    }

    public void Visit(FunCallStatement stmt)
    {
        string functionName = stmt.NameIdentifier.Value;
        if (!_namedValues.TryGetValue(functionName, out ValueRef function))
            throw new Exception("the function is not declared.");

        var args = new ValueRef[stmt.Parameters.Count];
        for (int i = 0; i < args.Length; i++)
        {
            stmt.Parameters[i].Accept(this);
            args[i] = _valueStack.Pop();
        }

        LLVM.BuildCall(_builder, function, args, functionName);
    }

    public void Visit(FuncDeclStatement stmt)
    {
        var paramsType = new TypeRef[stmt.Params.Count];
        for (int i = 0; i < paramsType.Length; i++)
        {
            paramsType[i] = ToLLVMType(stmt.Params[i].ParamType!);
        }

        var functionType = LLVM.FunctionType(ToLLVMType(stmt.ReturnType!), paramsType, false);

        if (stmt.IsExtern)
        {
            var function = LLVM.AddFunction(_module, stmt.Name, functionType);
            LLVM.SetLinkage(function, Linkage.ExternalLinkage);
            _namedValues[stmt.Name] = function;
            return;
        }

        var functionDef = LLVM.AddFunction(_module, stmt.Name, functionType);
        _namedValues[stmt.Name] = functionDef;

        for (int i = 0; i < paramsType.Length; i++)
        {
            string paramName = stmt.Params[i].NameIdentifier.Value;

            ValueRef param = LLVM.GetParam(functionDef, (uint)i);
            LLVM.SetValueName(param, paramName);

            _namedValues[paramName] = param;
        }

        BasicBlockRef entry = LLVM.AppendBasicBlock(functionDef, "entry");
        LLVM.PositionBuilderAtEnd(_builder, entry);

        stmt.Body?.Accept(this);
    }


    public void Visit(ReturnStatement stmt)
    {
        stmt.ReturnExpr.Accept(this);
        LLVM.BuildRet(_builder, _valueStack.Pop());
    }

    public void Visit(VarDeclStatement stmt)
    {
        var varType = ToLLVMType(stmt.VarType!);

        ValueRef alloca = LLVM.BuildAlloca(_builder, varType, stmt.VarName);

        if (stmt.InitExpression != null)
        {
            stmt.InitExpression.Accept(this);
            ValueRef initValue = _valueStack.Pop();
            LLVM.BuildStore(_builder, initValue, alloca);
        }

        _namedValues[stmt.VarName] = alloca;
    }

    public void Visit(LiteralExpression expr)
    {
        if (expr.LiteralToken.Type == TokenType.IntLiteral)
        {
            TypeRef type = LLVM.Int32Type();
            ValueRef value = LLVM.ConstInt(type, (ulong)int.Parse(expr.LiteralToken.Value), false);
            _valueStack.Push(value);
        }
        else if (expr.LiteralToken.Type == TokenType.StringLiteral)
        {
            var str = expr.LiteralToken.Value;
            ValueRef value = LLVM.BuildGlobalString(_builder, str, GetGlobalStrVar());
            
            _valueStack.Push(value);
        }
        else throw new NotImplementedException();
    }

    public void Visit(FunCallExpression expr)
    {
        if (!_namedValues.TryGetValue(expr.NameIdentifier.Value, out ValueRef function))
        {
            throw new Exception("fonction non déclarée.");
        }

        var args = new ValueRef[expr.Parameters.Count];
        for (int i = 0; i < args.Length; i++)
        {
            expr.Parameters[i].Accept(this);
            args[i] = _valueStack.Pop();
        }

        _valueStack.Push(LLVM.BuildCall(_builder, function, args, expr.NameIdentifier.Value));
    }

    public void Visit(BinaryOpExpression stmt)
    {
        stmt.Left.Accept(this);
        stmt.Right.Accept(this);

        var right = _valueStack.Pop();
        var left = _valueStack.Pop();

        switch (stmt.OpType)
        {
            case BinOperatorType.Addition:
                _valueStack.Push(LLVM.BuildAdd(_builder, left, right, "add_result"));
                break;
            case BinOperatorType.Subtraction:
                _valueStack.Push(LLVM.BuildSub(_builder, left, right, "sub_result"));
                break;
            case BinOperatorType.Multiplication:
                _valueStack.Push(LLVM.BuildMul(_builder, left, right, "mul_result"));
                break;
            case BinOperatorType.Division:
                _valueStack.Push(LLVM.BuildSDiv(_builder, left, right, "div_result"));
                break;
        }
    }

    public void Visit(VarRefExpression expr)
    {
        if (!_namedValues.TryGetValue(expr.NameIdentifier.Value, out ValueRef value))
        {
            throw new Exception("variable non déclarée.");
        }
        
        if (LLVM.GetTypeKind(LLVM.TypeOf(value)) == TypeKind.PointerTypeKind)
        {
            ValueRef loadedValue = LLVM.BuildLoad(_builder, value, expr.NameIdentifier.Value);
            _valueStack.Push(loadedValue);
        }
        else _valueStack.Push(value);
    }

    private string GetGlobalStrVar()
    {
        return $"globalStr{_globalStringCount++}";
    }

    private static TypeRef ToLLVMType(TypeSymbol type)
    {
        return type.LLVMType!.Value;
    }
}