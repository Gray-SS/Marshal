using Swigged.LLVM;
using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;
using Marshal.Compiler.Errors;
using Marshal.Compiler.Semantics;
using Marshal.Compiler.Utilities;

namespace Marshal.Compiler.IR;

public class IRGenerator : CompilerPass, IASTVisitor
{
    private int _globalStrCount;
    private ModuleRef _module;
    private BuilderRef _builder;
    private readonly LLVMTypeResolver _typeResolver;

    private readonly Stack<ValueRef> _valueStack;
    private readonly Dictionary<string, ValueRef> _namedValues;

    public IRGenerator(CompilationContext context, ErrorHandler errorHandler) : base(context, errorHandler)
    {
        _typeResolver = new LLVMTypeResolver();
        _valueStack = new Stack<ValueRef>();
        _namedValues = new Dictionary<string, ValueRef>();
    }

    public override void Apply()
    {
        _module = LLVM.ModuleCreateWithName(Context.FullPath);
        _builder = LLVM.CreateBuilder();

        Visit(Context.AST);
        Context.Module = _module;

        LLVM.DumpModule(Context.Module);

        var message = new MyString();
        LLVM.VerifyModule(_module, VerifierFailureAction.AbortProcessAction, message);
    }

    public void Visit(CompilationUnit unit)
    {
        foreach (var statement in unit.Statements)
        {
            statement.Accept(this);
        }
    }

    public void Visit(ScopeStatement stmt)
    {
        foreach (var statement in stmt.Statements)
        {
            statement.Accept(this);
        }
    }

    public void Visit(AssignmentStatement stmt)
    {
    }

    public void Visit(FunCallStatement stmt)
    {
        FunctionSymbol function = stmt.Symbol;
        CallFunction(function, stmt.Args);
    }

    public void Visit(FuncDeclStatement stmt)
    {
        FunctionSymbol function = stmt.Symbol;

        TypeRef retType = _typeResolver.Resolve(function.ReturnType);
        TypeRef[] paramTypes = function.Params.Select(x => _typeResolver.Resolve(x.DataType)).ToArray();

        TypeRef functionType = LLVM.FunctionType(retType, paramTypes, false);
        ValueRef fn = LLVM.AddFunction(_module, function.Name, functionType);
        _namedValues[function.Name] = fn;

        if (function.IsExtern)
            LLVM.SetLinkage(fn, Linkage.ExternalLinkage);

        for (int i = 0; i < function.Params.Count; i++)
        {
            VariableSymbol param = function.Params[i];
            ValueRef pValue = LLVM.GetParam(fn, (uint)i);
            LLVM.SetValueName(pValue, param.Name);

            _namedValues[param.Name] = pValue;
        }

        if (stmt.Body != null)
        {
            LLVM.PositionBuilderAtEnd(_builder, LLVM.AppendBasicBlock(fn, "entry"));

            stmt.Body.Accept(this);
        }
    }

    public void Visit(ReturnStatement stmt)
    {
        stmt.ReturnExpr.Accept(this);
        
        ValueRef value = _valueStack.Pop();
        LLVM.BuildRet(_builder, value);
    }

    public void Visit(VarDeclStatement stmt)
    {
        VariableSymbol variable = stmt.Symbol;

        TypeRef varType = _typeResolver.Resolve(variable.DataType);
        ValueRef varPtr = LLVM.BuildAlloca(_builder, varType, variable.Name);

        if (stmt.Initializer != null)
        {
            stmt.Initializer.Accept(this);
            ValueRef initializeValue = _valueStack.Pop();

            if (LLVM.GetTypeKind(LLVM.TypeOf(initializeValue)) == TypeKind.ArrayTypeKind &&
                LLVM.GetTypeKind(varType) == TypeKind.PointerTypeKind)
            {
                ValueRef zeroIndex = LLVM.ConstInt(LLVM.Int32Type(), 0, false);
                ValueRef arrayBaseAddress = LLVM.BuildGEP(
                    _builder,
                    initializeValue,
                    [ zeroIndex, zeroIndex ],
                    "array_ptr"
                );

                Console.WriteLine($"{variable.Name}");
                LLVM.BuildStore(_builder, arrayBaseAddress, varPtr);
            }
            else
            {
                Console.WriteLine($"{variable.Name}");
                LLVM.BuildStore(_builder, initializeValue, varPtr);
            }
        }

        _namedValues[variable.Name] = varPtr;
    }

    public void Visit(BinaryOpExpression expr)
    {
        expr.Left.Accept(this);
        ValueRef lValue = _valueStack.Pop();

        expr.Right.Accept(this);
        ValueRef rValue = _valueStack.Pop();

        ValueRef result = expr.OpType switch
        {
            BinOperatorType.Addition => LLVM.BuildAdd(_builder, lValue, rValue, "add_result"),
            BinOperatorType.Subtraction => LLVM.BuildSub(_builder, lValue, rValue, "sub_result"),
            BinOperatorType.Multiplication => LLVM.BuildMul(_builder, lValue, rValue, "mul_result"),
            BinOperatorType.Division => LLVM.BuildSDiv(_builder, lValue, rValue, "div_result"),
            _ => throw new NotImplementedException(),
        };

        _valueStack.Push(result);
    }

    public void Visit(FunCallExpression expr)
    {
        FunctionSymbol function = expr.Symbol;
        _valueStack.Push(CallFunction(function, expr.Args));
    }

    public void Visit(LiteralExpression expr)
    {
        switch (expr.LiteralType)
        {
            case LiteralType.Int:
            {
                int n = int.Parse(expr.Token.Value);
                _valueStack.Push(LLVM.ConstInt(LLVM.Int32Type(), (ulong)n, false));
            } break;

            case LiteralType.Boolean:
            {
                int n = expr.Token.Value == "true" ? 1 : 0;
                _valueStack.Push(LLVM.ConstInt(LLVM.Int32Type(), (ulong)n, false));
            } break;

            case LiteralType.String:
            {
                string str = expr.Token.Value;
                _valueStack.Push(LLVM.BuildGlobalStringPtr(_builder, str, GetGlobalStrName()));
            } break;
        }
    }

    public void Visit(VarRefExpression expr)
    {
        VariableSymbol var = expr.Symbol;

        ValueRef varValue = _namedValues[var.Name];
        if (LLVM.GetTypeKind(LLVM.TypeOf(varValue)) == TypeKind.PointerTypeKind)
        {
            ValueRef loadedValue = LLVM.BuildLoad(_builder, varValue, "var_loaded_value");
            _valueStack.Push(loadedValue);
        }
        else _valueStack.Push(varValue);
    }

    public void Visit(ArrayInitExpression expr)
    {
    }

    private ValueRef CallFunction(FunctionSymbol function, List<SyntaxExpression> args)
    {
        ValueRef fn = _namedValues[function.Name];

        ValueRef[] argsValue = args.Select(x => {
            x.Accept(this);
            return _valueStack.Pop();
        }).ToArray(); 

        return LLVM.BuildCall(_builder, fn, argsValue, $"{function.Name}_result");
    }

    private string GetGlobalStrName()
    {
        return $"globalStr{_globalStrCount++}";
    }
}