using Swigged.LLVM;
using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;
using Marshal.Compiler.Errors;
using Marshal.Compiler.Semantics;
using System.Buffers;

namespace Marshal.Compiler.IR;

public class Function
{
    public string Name => Symbol.Name;

    public FunctionSymbol Symbol { get; }
    public TypeRef ReturnType { get; }
    public ValueRef Pointer { get; }

    public Function(FunctionSymbol symbol, TypeRef returnType, ValueRef pointer)
    {
        Symbol = symbol;
        Pointer = pointer;
        ReturnType = returnType;
    }
}

public class Param
{
    public string Name { get; }
    public TypeRef Type { get; }
    public ValueRef Value { get; }

    public Param(string name, TypeRef type, ValueRef value)
    {
        Name = name;
        Type = type;
        Value = value;
    }
}

public class Variable
{
    public string Name => Symbol.Name;

    public VariableSymbol Symbol { get; }
    public TypeRef Type { get; }
    public ValueRef Pointer { get; set; }

    public Variable(VariableSymbol symbol, TypeRef type)
    {
        Symbol = symbol;
        Type = type;
    }

    public void SetPointer(ValueRef pointer)
    {
        Pointer = pointer;
    }

    public ValueRef Allocate(BuilderRef builder)
    {
        Pointer = LLVM.BuildAlloca(builder, Type, $"{Symbol.Name}_var");
        return Pointer;
    }

    public void Store(BuilderRef builder, ValueRef value)
    {
        LLVM.BuildStore(builder, value, Pointer);
    }

    public ValueRef Load(BuilderRef builder)
    {
        return LLVM.BuildLoad(builder, Pointer, $"{Symbol.Name}_load");
    }
}

public class IRGenerator : CompilerPass, IASTVisitor
{
    private int _globalStrCount;
    private ModuleRef _module;
    private BuilderRef _builder;
    private readonly LLVMTypeResolver _typeResolver;

    private readonly Stack<ValueRef> _valueStack;
    private readonly Dictionary<string, Param> _params;
    private readonly Dictionary<string, Variable> _variables;
    private readonly Dictionary<string, Function> _functions;

    public IRGenerator(CompilationContext context, ErrorHandler errorHandler) : base(context, errorHandler)
    {
        _typeResolver = new LLVMTypeResolver();
        _valueStack = new Stack<ValueRef>();

        _params = new Dictionary<string, Param>();
        _variables = new Dictionary<string, Variable>();
        _functions = new Dictionary<string, Function>();
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
        Variable variable = _variables[stmt.Symbol.Name];
        ValueRef lValue = EvaluateExpr(stmt.LValue, false);
        ValueRef value = EvaluateExpr(stmt.Initializer);

        if (stmt.LValue is ArrayAccessExpression)
        {
            LLVM.BuildStore(_builder, value, lValue);
        }
        else variable.Store(_builder, value);
    }

    public void Visit(FunCallStatement stmt)
    {
        FunctionSymbol function = stmt.Symbol;
        CallFunction(function, stmt.Args);
    }

    public void Visit(FuncDeclStatement stmt)
    {
        FunctionSymbol functionSymbol = stmt.Symbol;

        TypeRef retType = _typeResolver.Resolve(functionSymbol.ReturnType);
        TypeRef[] paramTypes = functionSymbol.Params.Select(x => _typeResolver.Resolve(x.DataType)).ToArray();

        TypeRef functionType = LLVM.FunctionType(retType, paramTypes, false);
        ValueRef fn = LLVM.AddFunction(_module, functionSymbol.Name, functionType);

        Function function = new Function(functionSymbol, retType, fn);
        _functions[function.Name] = function;

        if (functionSymbol.IsExtern)
            LLVM.SetLinkage(fn, Linkage.ExternalLinkage);

        for (int i = 0; i < functionSymbol.Params.Count; i++)
        {
            VariableSymbol paramSymbol = functionSymbol.Params[i];
            TypeRef paramType = _typeResolver.Resolve(paramSymbol.DataType);

            ValueRef pValue = LLVM.GetParam(fn, (uint)i);
            LLVM.SetValueName(pValue, paramSymbol.Name);

            Param param = new Param(paramSymbol.Name, paramType, pValue);
            _params[param.Name] = param;
        }

        if (stmt.Body != null)
        {
            LLVM.PositionBuilderAtEnd(_builder, LLVM.AppendBasicBlock(fn, "entry"));

            stmt.Body.Accept(this);
        }
    }

    public void Visit(ReturnStatement stmt)
    {
        ValueRef value = EvaluateExpr(stmt.ReturnExpr);
        
        LLVM.BuildRet(_builder, value);
    }

    public void Visit(VarDeclStatement stmt)
    {
        VariableSymbol variableSymbol = stmt.Symbol;

        TypeRef varType = _typeResolver.Resolve(variableSymbol.DataType);

        var variable = new Variable(variableSymbol, varType);
        variable.Allocate(_builder);

        if (stmt.Initializer != null)
        {
            ValueRef value = EvaluateExpr(stmt.Initializer);
            variable.Store(_builder, value);
        }

        _variables[variableSymbol.Name] = variable;
    }

    public void Visit(BinaryOpExpression expr)
    {
        ValueRef lValue = EvaluateExpr(expr.Left);
        ValueRef rValue = EvaluateExpr(expr.Right);

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
                _valueStack.Push(LLVM.ConstInt(LLVM.Int1Type(), (ulong)n, false));
            } break;

            case LiteralType.String:
            {
                string str = expr.Token.Value;
                _valueStack.Push(LLVM.BuildGlobalStringPtr(_builder, str, GetGlobalStrName()));
            } break;

            case LiteralType.Char:
            {
                int n = expr.Token.Value[0];
                _valueStack.Push(LLVM.ConstInt(LLVM.Int8Type(), (ulong)n, false));
            } break;
        }
    }

    public void Visit(VarRefExpression expr)
    {
        VariableSymbol var = expr.Symbol;

        if (_variables.TryGetValue(var.Name, out Variable? variable))
        {
            _valueStack.Push(variable.Load(_builder));
        }
        else if (_params.TryGetValue(var.Name, out Param? param))
        {
            _valueStack.Push(param.Value);
        }
        else 
        {
            throw new CompilerException(ErrorType.InternalError, "FATAL. The variable couldn't be found.");
        }
    }

    public void Visit(NewExpression expr)
    {
        if (expr is NewArrayExpression arrayExpr)
        {
            var arrayType = (ArrayType)arrayExpr.Type;
            var elementType = _typeResolver.Resolve(arrayType.ElementType);

            var lengthValue = EvaluateExpr(arrayExpr.LengthExpr);

            ValueRef arrayPtr = LLVM.BuildArrayMalloc(_builder, elementType, lengthValue, $"array_malloc");
            _valueStack.Push(arrayPtr);
        }
    }

    public void Visit(ArrayAccessExpression expr)
    {
        ValueRef indexorPtr = EvaluateExpr(expr.ArrayExpr);
        ValueRef indexValue = EvaluateExpr(expr.IndexExpr);

        ValueRef elementPtr = LLVM.BuildGEP(_builder, indexorPtr, [ indexValue ], "array_iptr");
        _valueStack.Push(elementPtr);
    }

    public void Visit(ArrayInitExpression expr)
    {
        var values = expr.Expressions.Select(x => {
            x.Accept(this);
            return _valueStack.Pop();
        }).ToArray();

        TypeRef type = _typeResolver.Resolve(expr.Type); 
        TypeRef elementType = LLVM.GetElementType(type);

        ValueRef length = LLVM.ConstInt(LLVM.Int32Type(), (ulong)values.Length, false);
        ValueRef array = LLVM.ConstArray(elementType, values);

        _valueStack.Push(array);
    }

    private ValueRef CallFunction(FunctionSymbol function, List<SyntaxExpression> args)
    {
        Function fn = _functions[function.Name];

        ValueRef[] argsValue = args.Select(x => {
            x.Accept(this);
            return _valueStack.Pop();
        }).ToArray(); 

        return LLVM.BuildCall(_builder, fn.Pointer, argsValue, $"{function.Name}_result");
    }

    private ValueRef EvaluateExpr(SyntaxExpression expr, bool rValue = true)
    {
        expr.Accept(this);
        ValueRef value = _valueStack.Pop();

        if (rValue && expr is ArrayAccessExpression)
            value = LLVM.BuildLoad(_builder, value, $"rvalue");

        return value;
    }

    private string GetGlobalStrName()
    {
        return $"globalStr{_globalStrCount++}";
    }
}