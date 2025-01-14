using Swigged.LLVM;
using Marshal.Core.Syntax;
using Marshal.Core.Syntax.Expressions;
using Marshal.Core.Syntax.Statements;
using Marshal.Core.Errors;
using Marshal.Core.Semantics;
using System.Buffers;
using System.Diagnostics;
using Marshal.Core.Visitors;
using Marshal.Core;

namespace Marshal.Backend.IR;

public class Struct 
{
    public string Name => Symbol.Name;

    public StructType Symbol { get; }
    public TypeRef Type { get; }
    public TypeRef[] Fields { get; }

    public Struct(StructType symbol, TypeRef type, TypeRef[] fields)
    {
        Symbol = symbol;
        Type = type;
        Fields = fields;
    }
}

public class Function
{
    public string Name => Symbol.Name;

    public FunctionSymbol Symbol { get; }
    public TypeRef ReturnType { get; }
    public ValueRef Pointer { get; }
    public ValueRef ReturnPointer { get; set; }
    public BasicBlockRef ReturnBlock { get; set; }

    public Function(FunctionSymbol symbol, TypeRef returnType, ValueRef pointer)
    {
        Symbol = symbol;
        Pointer = pointer;
        ReturnType = returnType;
    }
}

public interface INamedValue 
{
    string Name { get; }
    TypeRef Type { get; }
    ValueRef Pointer { get; set; }

    ValueRef Load(BuilderRef builder);
    ValueRef GetDataPointer(BuilderRef builder);
}

public class Param : INamedValue
{
    public string Name => Symbol.Name;
    public TypeRef Type { get; }
    public ValueRef Pointer { get; set; }
    public VariableSymbol Symbol { get; }

    public Param(VariableSymbol symbol, TypeRef type, ValueRef pointer)
    {
        Symbol = symbol;
        Type = type;
        Pointer = pointer;
    }

    public ValueRef GetDataPointer(BuilderRef builder)
    {
        ValueRef pointer = Pointer;

        if (Symbol.DataType.IsReferenced || Symbol.DataType == MarshalType.String)
            pointer = LLVM.BuildLoad(builder, pointer, "malloc_ptr");

        return pointer;
    }

    public ValueRef Load(BuilderRef builder)
    {
        ValueRef value = LLVM.BuildLoad(builder, Pointer, $"param_load");
        return value;
    }
}

public class Variable : INamedValue
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

    public ValueRef Allocate(BuilderRef builder)
    {
        Pointer = LLVM.BuildAlloca(builder, Type, $"{Symbol.Name}_var");
        return Pointer;
    }

    public void Store(BuilderRef builder, ValueRef value)
    {
        if (Symbol.DataType.IsNumeric && LLVM.TypeOf(value) != Type) 
        {
            value = LLVM.BuildTrunc(builder, value, Type, $"{Symbol.Name}_trunc");
        }

        LLVM.BuildStore(builder, value, Pointer);
    }

    public ValueRef GetDataPointer(BuilderRef builder)
    {
        ValueRef pointer = Pointer;

        if (Symbol.DataType.IsReferenced || Symbol.DataType == MarshalType.String)
            pointer = LLVM.BuildLoad(builder, pointer, "malloc_ptr");

        return pointer;
    }

    public ValueRef Load(BuilderRef builder)
    {
        ValueRef value = LLVM.BuildLoad(builder, Pointer, $"{Symbol.Name}_load");

        if (Symbol.DataType.IsReferenced || Symbol.DataType == MarshalType.String)
            value = LLVM.BuildLoad(builder, value, "malloc_ptr");

        return value;
    }
}

public class IRGenerator : CompilerPass, IASTVisitor
{
    private const string FUNCTION_RET_VAR_NAME = "ret_value";

    private int _globalStrCount;
    private ContextRef _contextRef;

    private LLVMModule _module = null!;
    private LLVMTypeResolver _typeResolver = null!;

    private ValueRef ZeroInt;
    private ValueRef OneInt; 
    private ValueRef MinusOneInt;


    private Function? _crntFn;
    private readonly LLVMContext _context;
    private readonly Stack<ValueRef> _valueStack;
    private readonly Dictionary<string, INamedValue> _variables;
    private readonly Dictionary<string, Function> _functions;
    private readonly Dictionary<string, Struct> _structs;

    private readonly CompilationUnit _unit;

    public IRGenerator(LLVMContext context, CompilationUnit unit, CompilationContext ccontext, ErrorHandler errorHandler) : base(ccontext, errorHandler)
    {
        _variables = new Dictionary<string, INamedValue>();
        _functions = new Dictionary<string, Function>();
        _structs = new Dictionary<string, Struct>();
        _unit = unit;
        _context = context;

        _valueStack = new Stack<ValueRef>();
    }

    public BackendModule Generate()
    {
        LLVMModule module = _context.CreateModule(Context.RelativePath);

        _typeResolver = new LLVMTypeResolver(_contextRef, _structs);

        TypeRef intType = _typeResolver.Resolve(MarshalType.Int);
        ZeroInt = LLVM.ConstInt(intType, 0, true);
        OneInt = LLVM.ConstInt(intType, 1, true);
        MinusOneInt = LLVM.ConstInt(intType, 0xFFFFFFFF, true);

        _unit.Accept(this);

        LLVM.DumpModule(module.ModuleRef);

        var message = new MyString();
        LLVM.VerifyModule(module.ModuleRef, VerifierFailureAction.AbortProcessAction, message);
        LLVM.DisposeBuilder(module.BuilderRef);

        return module;
    }

    public void Visit(CompilationUnit unit)
    {
        foreach (var statement in unit.Statements)
        {
            statement.Accept(this);
        }
    }

    public void Visit(IfStatement stmt)
    {
        ValueRef fn = LLVM.GetBasicBlockParent(LLVM.GetInsertBlock(_module.BuilderRef));
        BasicBlockRef mergeBB = LLVM.AppendBasicBlock(fn, "merge");

        foreach (var item in stmt.IfsScopes)
        {
            BasicBlockRef thenBB = LLVM.AppendBasicBlock(fn, "then");
            BasicBlockRef elseBB = LLVM.AppendBasicBlock(fn, "else");

            ValueRef cond = EvaluateExpr(item.ConditionExpr);
            LLVM.BuildCondBr(_module.BuilderRef, cond, thenBB, elseBB);

            LLVM.PositionBuilderAtEnd(_module.BuilderRef, thenBB);
            item.Scope.Accept(this);

            if (!item.Scope.IsReturning)
                LLVM.BuildBr(_module.BuilderRef, mergeBB);

            LLVM.PositionBuilderAtEnd(_module.BuilderRef, elseBB);
        }

        stmt.ElseScope?.Accept(this);
        if (stmt.ElseScope == null || !stmt.ElseScope.IsReturning)
            LLVM.BuildBr(_module.BuilderRef, mergeBB);

        LLVM.PositionBuilderAtEnd(_module.BuilderRef, mergeBB);
    }

    public void Visit(WhileStatement stmt)
    {
        ValueRef fn = LLVM.GetBasicBlockParent(LLVM.GetInsertBlock(_module.BuilderRef));
        BasicBlockRef mergeBB = LLVM.AppendBasicBlock(fn, "merge");
        BasicBlockRef preBB = LLVM.AppendBasicBlock(fn, "pre");
        BasicBlockRef thenBB = LLVM.AppendBasicBlock(fn, "then");

        LLVM.BuildBr(_module.BuilderRef, preBB);

        LLVM.PositionBuilderAtEnd(_module.BuilderRef, preBB);
        ValueRef condValue = EvaluateExpr(stmt.CondExpr); 
        LLVM.BuildCondBr(_module.BuilderRef, condValue, thenBB, mergeBB);

        LLVM.PositionBuilderAtEnd(_module.BuilderRef, thenBB);
        stmt.Scope.Accept(this);

        if (!stmt.Scope.IsReturning)
            LLVM.BuildBr(_module.BuilderRef, preBB);

        LLVM.PositionBuilderAtEnd(_module.BuilderRef, mergeBB);
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
        ValueRef lValue = EvaluateExpr(stmt.LExpr, loadLocator: false);
        ValueRef value = EvaluateExpr(stmt.Initializer);

        CastKind kind = stmt.Initializer!.Type.GetCastKind(stmt.LExpr.Type);
        if (kind == CastKind.Implicit)
            value = CastValue(value, stmt.Initializer.Type, stmt.LExpr.Type);

        LLVM.BuildStore(_module.BuilderRef, value, lValue);
    }

    public void Visit(IncrementStatement stmt)
    {
        INamedValue variable = _variables[stmt.Symbol.Name];

        if (!stmt.Symbol.DataType.IsPointer)
        {
            ValueRef result;
            
            ValueRef varValue = variable.Load(_module.BuilderRef);

            //TODO: Set the sign extend flag relative to the variable type
            ValueRef one = LLVM.ConstInt(variable.Type, 1, true);
            if (stmt.Decrement) result = LLVM.BuildSub(_module.BuilderRef, varValue, one, "inc_result");
            else result = LLVM.BuildAdd(_module.BuilderRef, varValue, one, "inc_result");

            LLVM.BuildStore(_module.BuilderRef, result, variable.Pointer);
        }
        else
        {
            // Allowing incrementation of pointers for the following example:
            // var a: int[] = new int[2];
            // a[0] = 5;
            // a[1] = 10;

            // var p: int* = &a[0];
            // p++;
            //
            // we need to load the address that the pointer is pointing to
            // next, we're getting the next element of the pointed type.
            // finally we're storing the new pointed memory to the pointer variable

            ValueRef ptr = variable.Load(_module.BuilderRef);
            if (!stmt.Decrement) ptr = LLVM.BuildGEP(_module.BuilderRef, ptr, [ OneInt ], "inc_result"); 
            else ptr = LLVM.BuildGEP(_module.BuilderRef, ptr, [ MinusOneInt ], "dec_result");

            LLVM.BuildStore(_module.BuilderRef, ptr, variable.Pointer);
        }
    }

    public void Visit(FunCallStatement stmt)
    {
        FunctionSymbol function = stmt.Symbol;
        CallFunction(function, stmt.Args);
    }

    public void Visit(StructDeclStatement stmt)
    {
        StructType symbol = stmt.Symbol;

        TypeRef[] fieldTypes = symbol.Fields.Select(f => _typeResolver.Resolve(f.DataType)).ToArray();
        TypeRef structType = LLVM.StructCreateNamed(_context.ContextRef, stmt.Identifier.Value);
        LLVM.StructSetBody(structType, fieldTypes, false);

        var @struct = new Struct(symbol, structType, fieldTypes); 
        _structs.Add(@struct.Name, @struct);
    }

    public void Visit(FieldDeclStatement stmt)
    {   
    }

    public void Visit(FuncDeclStatement stmt)
    {
        FunctionSymbol functionSymbol = stmt.Symbol;

        TypeRef retType = _typeResolver.Resolve(functionSymbol.ReturnType);
        TypeRef[] paramTypes = functionSymbol.Params.Select(x => _typeResolver.Resolve(x.DataType)).ToArray();

        TypeRef functionType = LLVM.FunctionType(retType, paramTypes, false);
        ValueRef fn = LLVM.AddFunction(_module.ModuleRef, functionSymbol.Name, functionType);

        Function function = new Function(functionSymbol, retType, fn);
        _functions[function.Name] = function;

        if (functionSymbol.IsExtern)
            LLVM.SetLinkage(fn, Linkage.ExternalLinkage);

        if (stmt.Body != null)
        {
            _crntFn = function;
            BasicBlockRef entryBB = LLVM.AppendBasicBlock(fn, "entry");

            LLVM.PositionBuilderAtEnd(_module.BuilderRef, entryBB);
            for (int i = 0; i < functionSymbol.Params.Count; i++)
            {
                VariableSymbol paramSymbol = functionSymbol.Params[i];
                TypeRef paramType = _typeResolver.Resolve(paramSymbol.DataType);

                ValueRef paramValue = LLVM.GetParam(fn, (uint)i);
                ValueRef paramPtr = LLVM.BuildAlloca(_module.BuilderRef, paramType, $"{paramSymbol.Name}_ptr");
                LLVM.BuildStore(_module.BuilderRef, paramValue, paramPtr);

                var param = new Param(paramSymbol, paramType, paramPtr);
                _variables[param.Name] = param;
            }

            if (functionSymbol.ReturnType != MarshalType.Void)
            {
                ValueRef returnPtr = LLVM.BuildAlloca(_module.BuilderRef, retType, FUNCTION_RET_VAR_NAME);
                function.ReturnPointer = returnPtr;
            }

            BasicBlockRef returnBB = LLVM.AppendBasicBlock(fn, "return");
            function.ReturnBlock = returnBB;

            LLVM.PositionBuilderAtEnd(_module.BuilderRef, returnBB);

            if (functionSymbol.ReturnType != MarshalType.Void)
            {
                var retValue = LLVM.BuildLoad(_module.BuilderRef, function.ReturnPointer, "ret_value");
                LLVM.BuildRet(_module.BuilderRef, retValue);
            }
            else LLVM.BuildRetVoid(_module.BuilderRef);

            LLVM.PositionBuilderAtEnd(_module.BuilderRef, entryBB); 

            stmt.Body.Accept(this);

            _crntFn = default;

            if (!stmt.Body.IsReturning)
                LLVM.BuildBr(_module.BuilderRef, returnBB);
        }
    }

    public void Visit(ReturnStatement stmt)
    {
        if (_crntFn == null) {
            Report(ErrorType.Error, "Returning should only be done within a function.");
            return;
        }

        if (_crntFn.Symbol.ReturnType != MarshalType.Void)
        {
            ValueRef value = EvaluateExpr(stmt.ReturnExpr!);

            CastKind kind = stmt.ReturnExpr!.Type.GetCastKind(_crntFn.Symbol.ReturnType);
            if (kind == CastKind.Implicit)
                value = CastValue(value, stmt.ReturnExpr.Type, _crntFn.Symbol.ReturnType);

            LLVM.BuildStore(_module.BuilderRef, value, _crntFn.ReturnPointer);
        }

        LLVM.BuildBr(_module.BuilderRef, _crntFn.ReturnBlock);
    }

    public void Visit(VarDeclStatement stmt)
    {
        VariableSymbol variableSymbol = stmt.Symbol;

        TypeRef varType = _typeResolver.Resolve(variableSymbol.DataType);

        var variable = new Variable(variableSymbol, varType);
        variable.Allocate(_module.BuilderRef);

        if (stmt.Initializer != null)
        {
            ValueRef value = EvaluateExpr(stmt.Initializer);

            CastKind kind = stmt.Initializer.Type.GetCastKind(stmt.Symbol.DataType);
            if (kind == CastKind.Implicit)
                value = CastValue(value, stmt.Initializer.Type, stmt.Symbol.DataType);

            variable.Store(_module.BuilderRef, value);
        }

        _variables[variableSymbol.Name] = variable;
    }

    public void Visit(CastExpression expr)
    {
        ValueRef operand = EvaluateExpr(expr.CastedExpr);

        // Déterminez les types source et cible
        MarshalType sourceType = expr.CastedExpr.Type;
        MarshalType targetType = expr.Type;

        if (sourceType == targetType)
        {
            _valueStack.Push(operand);
            return;
        }

        ValueRef result = CastValue(operand, sourceType, targetType);
        _valueStack.Push(result);
    }

    private ValueRef CastValue(ValueRef operand, MarshalType sourceType, MarshalType targetType)
    {
        TypeRef targetLLVMType = _typeResolver.Resolve(targetType);

        CastOperation operation = sourceType.GetCastOperation(targetType);
        Debug.Assert(operation != CastOperation.Invalid, $"an invalid cast operation was found ({sourceType.Name}) -> ({targetType.Name}).");

        return operation switch
        {
            CastOperation.Identity => operand,
            CastOperation.Bitcast => LLVM.BuildBitCast(_module.BuilderRef, operand, targetLLVMType, "bitcast_tmp"),
            CastOperation.SignExtend => LLVM.BuildSExt(_module.BuilderRef, operand, targetLLVMType, "sext_tmp"),
            CastOperation.ZeroExtend => LLVM.BuildZExt(_module.BuilderRef, operand, targetLLVMType, "zext_tmp"),
            CastOperation.Truncate => LLVM.BuildTrunc(_module.BuilderRef, operand, targetLLVMType, "trunc_tmp"),
            CastOperation.Float2SInt => LLVM.BuildFPToSI(_module.BuilderRef, operand, targetLLVMType, "fptosi_tmp"),
            CastOperation.Float2UInt => LLVM.BuildFPToUI(_module.BuilderRef, operand, targetLLVMType, "fptoui_tmp"),
            CastOperation.SInt2Float => LLVM.BuildSIToFP(_module.BuilderRef, operand, targetLLVMType, "sitofp_tmp"),
            CastOperation.UInt2Float => LLVM.BuildUIToFP(_module.BuilderRef, operand, targetLLVMType, "uitofp_tmp"),
            CastOperation.FloatTrunc => LLVM.BuildFPTrunc(_module.BuilderRef, operand, targetLLVMType, "fptrunc_tmp"),
            CastOperation.FloatExt => LLVM.BuildFPExt(_module.BuilderRef, operand, targetLLVMType, "fpext_temp"),
            _ => throw new NotImplementedException($"The cast operation '{operation}' is currently not supported."),
        };
    }


    public void Visit(UnaryOpExpression expr)
    {
        // Key differences to allow complex address of '&' and deference '*' operators :
        // - To allow *(&x):
        // &x is an unary operation and is evaluated to a transient (rvalue).
        // since it's a transient value we don't need to load the pointer
        //
        // - To allow *p where p is a pointer:
        // p is a var ref expression and is evaluated to a locator (lvalue).
        // since it's a locator value we need to load the locator first.
        
        // Determine if the expression should return an lvalue (pointer) or load its value.
        // If the operation is AddressOf or we are evaluating for an lvalue, do not load.
        bool loadLocator = expr.Operation != UnaryOpType.AddressOf && expr.Operation != UnaryOpType.Deference;
        ValueRef operand = EvaluateExpr(expr.Operand, loadLocator);

        ValueRef result;
        switch (expr.Operation)
        {
            case UnaryOpType.Negation:
                result = LLVM.BuildNeg(_module.BuilderRef, operand, "negTmp");
                break;

            case UnaryOpType.Not:
                result = LLVM.BuildNot(_module.BuilderRef, operand, "notTmp");
                break;

            case UnaryOpType.AddressOf:
                result = operand;
                break;

            case UnaryOpType.Deference:
                if (expr.Operand.ValueCategory == ValueCategory.Locator) result = LLVM.BuildLoad(_module.BuilderRef, operand, "defTmp");
                else result = operand;
                break;

            default:
                throw new InvalidOperationException($"Unsupported unary operation: {expr.Operation}");
        }

        _valueStack.Push(result);
    }

    public void Visit(BracketExpression expr)
    {
        ValueRef value = EvaluateExpr(expr.Expression, false);
        _valueStack.Push(value);
    }

    public void Visit(BinaryOpExpression expr)
    {
        //We know that either the left or the right expression is a pointer (check done in Semantic Analyzer pass).
        ValueRef result;
        if (expr.Left.Type.IsPointer || expr.Right.Type.IsPointer)
        {
            //Pointer arithmetics
            (ValueRef ptr, ValueRef numeric) = expr.Left.Type.IsPointer ? 
                                              (EvaluateExpr(expr.Left), EvaluateExpr(expr.Right)) :
                                              (EvaluateExpr(expr.Right), EvaluateExpr(expr.Left));

            if (expr.Operation == BinOpType.Addition)
            {
                result = LLVM.BuildGEP(_module.BuilderRef, ptr, [ numeric ], "ptra_result");
            }
            else if (expr.Operation == BinOpType.Subtraction)
            {
                result = LLVM.BuildGEP(_module.BuilderRef, ptr, [ LLVM.BuildNeg(_module.BuilderRef, numeric, "neg_numeric") ], "ptr_sub_result");
            }
            else
                throw new InvalidOperationException($"Operation '{expr.Operation}' is not supported for pointer arithmetics.");
        }
        else
        {
            //Basic arithmetics
            ValueRef left = EvaluateExpr(expr.Left);
            ValueRef right = EvaluateExpr(expr.Right);
            MarshalType leftType = expr.Left.Type;
            MarshalType rightType = expr.Right.Type;

            if (expr.Operation.IsComparisonOperation())
            {
                if (leftType != rightType)
                {
                    if (leftType.SizeInBytes > rightType.SizeInBytes)
                        right = CastValue(right, rightType, leftType);
                    else if (leftType.SizeInBytes < rightType.SizeInBytes)
                        left = CastValue(left, leftType, rightType);
                    else
                        left = CastValue(left, leftType, rightType);
                }
            }
            else
            {
                if (leftType != expr.Type)
                    left = CastValue(left, leftType, expr.Type);
                if (rightType != expr.Type)
                    right = CastValue(right, rightType, expr.Type);
            }

            result = expr.Operation switch
            {
                BinOpType.Addition => LLVM.BuildAdd(_module.BuilderRef, left, right, "add_result"),
                BinOpType.Subtraction => LLVM.BuildSub(_module.BuilderRef, left, right, "sub_result"),
                BinOpType.Multiplication => LLVM.BuildMul(_module.BuilderRef, left, right, "mul_result"),
                BinOpType.Division => LLVM.BuildSDiv(_module.BuilderRef, left, right, "div_result"),
                BinOpType.Modulo => LLVM.BuildSRem(_module.BuilderRef, left, right, "mod_result"),
                BinOpType.Equals => LLVM.BuildICmp(_module.BuilderRef, IntPredicate.IntEQ, left, right, "eq_result"),
                BinOpType.NotEquals => LLVM.BuildICmp(_module.BuilderRef, IntPredicate.IntNE, left, right, "ne_result"),
                BinOpType.BiggerThan => LLVM.BuildICmp(_module.BuilderRef, IntPredicate.IntSGT, left, right, "gt_result"),
                BinOpType.BiggerThanEq => LLVM.BuildICmp(_module.BuilderRef, IntPredicate.IntSGE, left, right, "ge_result"),
                BinOpType.LessThan => LLVM.BuildICmp(_module.BuilderRef, IntPredicate.IntSLT, left, right, "lt_result"),
                BinOpType.LessThanEq => LLVM.BuildICmp(_module.BuilderRef, IntPredicate.IntSLE, left, right, "le_result"),

                _ => throw new NotImplementedException($"Unsupported binary operation: {expr.Operation}"),
            };
        }

        _valueStack.Push(result);
    }

    public void Visit(FunCallExpression expr)
    {
        System.Console.WriteLine("Function call expression");

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
                _valueStack.Push(LLVM.ConstInt(_typeResolver.Resolve(MarshalType.Int), (ulong)n, false));
            } break;

            case LiteralType.Boolean:
            {
                int n = expr.Token.Value == "true" ? 1 : 0;
                _valueStack.Push(LLVM.ConstInt(_typeResolver.Resolve(MarshalType.Boolean), (ulong)n, false));
            } break;

            case LiteralType.String:
            {
                string str = expr.Token.Value;
                _valueStack.Push(LLVM.BuildGlobalStringPtr(_module.BuilderRef, str, GetGlobalStrName()));
            } break;

            case LiteralType.Char:
            {
                int n = expr.Token.Value[0];
                _valueStack.Push(LLVM.ConstInt(_typeResolver.Resolve(MarshalType.Char), (ulong)n, false));
            } break;
        }
    }

    public void Visit(VarRefExpression expr)
    {
        VariableSymbol var = expr.Symbol;

        if (_variables.TryGetValue(var.Name, out INamedValue? variable))
        {
            ValueRef pointer = variable.GetDataPointer(_module.BuilderRef);
            _valueStack.Push(pointer);
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

            ValueRef arrayPtr = LLVM.BuildArrayMalloc(_module.BuilderRef, elementType, lengthValue, $"array_malloc");
            _valueStack.Push(arrayPtr);
        }
        else if (expr is NewStructExpression typeExpr)
        {
            //Do nothing.

            // var structType = (StructType)typeExpr.Type;
            // var structTypeRef = _typeResolver.Resolve(structType);

            // var values = structType.Fields.Select(_ => LLVM.ConstInt(_typeResolver.Resolve(MarshalType.Int), 0, true)).ToArray();

            // ValueRef structPtr = LLVM.ConstStructInContext(_context, values, false);
            // _valueStack.Push(structPtr);
        }
    }

    public void Visit(MemberAccessExpression expr)
    {
        ValueRef varPtr = EvaluateExpr(expr.VarExpr, false);
        ValueRef memberPtr = LLVM.BuildStructGEP(_module.BuilderRef, varPtr, (uint)expr.MemberIdx, "member_ptr");

        _valueStack.Push(memberPtr);
    }

    public void Visit(ArrayAccessExpression expr)
    {
        // To access an array at a certain index we need some flexibility.
        // For the simplest example: x[0] we need to return a pointer to the first element of x
        // In this context, x is simply an array of int.
        //
        // Since x is an array, it's a reference type, meaning it has a malloc address stored in the variable

        ValueRef indexorPtr = EvaluateExpr(expr.ArrayExpr, false);
        ValueRef indexValue = EvaluateExpr(expr.IndexExpr);

        ValueRef elementPtr = LLVM.BuildGEP(_module.BuilderRef, indexorPtr, [ indexValue ], "array_iptr");
        _valueStack.Push(elementPtr);
    }

    public void Visit(ArrayInitExpression expr)
    {
        ValueRef[] values = expr.Expressions.Select((x) => {
            return EvaluateExpr(x);
        }).ToArray();

        TypeRef type = _typeResolver.Resolve(expr.Type); 
        TypeRef elementType = LLVM.GetElementType(type);

        ValueRef length = LLVM.ConstInt(_typeResolver.Resolve(MarshalType.Int), (ulong)values.Length, false);
        ValueRef array = LLVM.ConstArray(elementType, values);

        _valueStack.Push(array);
    }

    private ValueRef CallFunction(FunctionSymbol function, List<SyntaxExpression> args)
    {
        Function fn = _functions[function.Name];

        ValueRef[] argsValue = new ValueRef[args.Count];
        for (int i = 0; i < args.Count; i++)
        {
            MarshalType dt = function.Params[i].DataType;
            
            bool loadLocator = !dt.IsReferenced;
            argsValue[i] = EvaluateExpr(args[i], true);
        }

        string returnName = function.ReturnType == MarshalType.Void ? string.Empty : $"{function.Name}_result"; 
        return LLVM.BuildCall(_module.BuilderRef, fn.Pointer, argsValue, returnName);
    }

    private ValueRef EvaluateExpr(SyntaxExpression expr, bool loadLocator = true)
    {
        expr.Accept(this);
        ValueRef value = _valueStack.Pop();

        if (expr.ValueCategory == ValueCategory.Locator && loadLocator)
        {
            value = LLVM.BuildLoad(_module.BuilderRef, value, "locator_load");
        }

        return value;
    }   

    private string GetGlobalStrName()
    {
        return $"globalStr{_globalStrCount++}";
    }
}