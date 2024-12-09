using Marshal.Compiler.Errors;
using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;
using Marshal.Compiler.Utilities;

namespace Marshal.Compiler.Semantics;

public class SymbolTableBuilder : CompilerPass, IASTVisitor
{
    public SymbolTableBuilder(CompilationContext context, ErrorHandler errorHandler) : base(context, errorHandler)
    {
    }

    public override void Apply()
    {
        Visit(Context.AST);
    }

    public void Visit(CompilationUnit unit)
    {
        DoVerifiedBlock(() => {
            foreach (SyntaxStatement statement in unit.Statements)
            {
                statement.Accept(this);
            }
        });
    }

    public void Visit(ScopeStatement stmt)
    {
        Context.SymbolTable.EnterScope();

        DoVerifiedBlock(() => {
            foreach (SyntaxStatement statement in stmt.Statements)
            {
                statement.Accept(this);
            }
        });

        Context.SymbolTable.ExitScope();
    }

    public void Visit(FuncDeclStatement stmt)
    {
        string functionName = stmt.NameToken.Value;

        if (Context.SymbolTable.TryGetFunction(functionName, out FunctionSymbol? existingSymbol))
        {
            if (existingSymbol.IsDefined)
                throw new CompilerDetailedException(ErrorType.SemanticError, $"une fonction nommée '{functionName}' est déjà définie dans ce contexte.", stmt.NameToken.Loc);

            if (!ValidateFunctionSignature(existingSymbol, stmt))
                throw new CompilerDetailedException(ErrorType.SemanticError, $"la fonction '{functionName}' est redéclarée avec une signature différente.", stmt.NameToken.Loc);

            if (stmt.Body != null)
            {
                existingSymbol.IsDefined = true;
                DefineFunctionBody(stmt, existingSymbol);
            }

            stmt.Symbol = existingSymbol;
            return;
        }

        var args = new List<VariableSymbol>();
        foreach (var param in stmt.Params)
        {
            var type = ResolveType(param.SyntaxType);
            args.Add(new VariableSymbol(param.NameToken.Value, type, true));
        }

        bool isDefined = stmt.Body != null;
        var returnType = ResolveType(stmt.SyntaxReturnType);

        var function = new FunctionSymbol(functionName, stmt.IsExtern, isDefined, returnType, args);
        Context.SymbolTable.AddSymbol(function);

        if (stmt.Body != null)
        {
            DefineFunctionBody(stmt, function);
        }

        stmt.Symbol = function;
    }

    private bool ValidateFunctionSignature(FunctionSymbol existingFunction, FuncDeclStatement newDeclaration)
    {
        if (existingFunction.ReturnType != ResolveType(newDeclaration.SyntaxReturnType))
            return false;

        if (existingFunction.Params.Count != newDeclaration.Params.Count)
            return false;

        for (int i = 0; i < existingFunction.Params.Count; i++)
        {
            var existingParam = existingFunction.Params[i];
            var newParam = newDeclaration.Params[i];

            if (existingParam.DataType != ResolveType(newParam.SyntaxType))
                return false;
        }

        return true;
    }

    private void DefineFunctionBody(FuncDeclStatement stmt, FunctionSymbol function)
    {
        Context.SymbolTable.EnterScope();

        try
        {
            foreach (var arg in function.Params)
            {
                Context.SymbolTable.AddSymbol(arg);
            }

            stmt.Body!.Accept(this);
        }
        finally
        {
            Context.SymbolTable.ExitScope();
        }
    }

    
    public void Visit(VarDeclStatement stmt)
    {
        string variableName = stmt.NameToken.Value;
        if (Context.SymbolTable.HasSymbol(variableName, SymbolType.Variable))
            throw new CompilerDetailedException(ErrorType.SemanticError, $"une variable nommée '{variableName}' existe déjà dans le contexte actuel.", stmt.NameToken.Loc);

        bool isDefined = stmt.Initializer != null;

        var type = ResolveType(stmt.SyntaxType);
        stmt.Initializer?.Accept(this);

        var variable = new VariableSymbol(variableName, type, isDefined);
        Context.SymbolTable.AddSymbol(variable);

        stmt.Symbol = variable;
    }

    public void Visit(AssignmentStatement stmt)
    {
        string variableName = stmt.NameToken.Value;
        if (!Context.SymbolTable.TryGetVariable(variableName, out VariableSymbol? symbol))
            throw new CompilerDetailedException(ErrorType.SemanticError, $"une variable nommée '{variableName}' existe déjà dans le contexte actuel.", stmt.NameToken.Loc);

        stmt.Initializer.Accept(this);
        stmt.Symbol = symbol;
    }

    public void Visit(FunCallStatement stmt)
    {
        string functionName = stmt.NameToken.Value;
        if (!Context.SymbolTable.TryGetFunction(functionName, out FunctionSymbol? symbol))
            throw new CompilerDetailedException(ErrorType.SemanticError, $"la fonction '{functionName}' n'est pas déclarée.", stmt.NameToken.Loc);

        if (!symbol.IsExtern && !symbol.IsDefined)
            throw new CompilerDetailedException(ErrorType.SemanticError, $"la fonction '{functionName}' n'est pas définie.", stmt.NameToken.Loc);

        if (!VerifyCallFunctionArgs(stmt.Args))
            return;

        stmt.Symbol = symbol;
    }

    public void Visit(ReturnStatement stmt)
    {
        stmt.ReturnExpr.Accept(this);
    }


    public void Visit(BinaryOpExpression expr)
    {
        expr.Left.Accept(this);
        expr.Right.Accept(this);

        if (expr.OpType.IsNumericBinOpType())
        {
            if (!expr.Left.Type.IsNumeric || !expr.Right.Type.IsNumeric)
                throw new CompilerException(ErrorType.SemanticError, $"opération invalide '{expr.OpType}' entre type non numérique '{expr.Left.Type.Name}' et '{expr.Right.Type.Name}'.");
        }
        else throw new NotImplementedException();

        expr.Type = GetWiderType(expr.Left.Type, expr.Right.Type);
    }

    public void Visit(VarRefExpression expr)
    {
        string variableName = expr.NameToken.Value;

        if (!Context.SymbolTable.TryGetVariable(variableName, out VariableSymbol? symbol))
            throw new CompilerDetailedException(ErrorType.SemanticError, $"la variable '{variableName}' n'est pas déclarée.", expr.NameToken.Loc);

        if (!symbol.IsInitialized)
            throw new CompilerDetailedException(ErrorType.SemanticError, $"la variable '{variableName}' a été utilisée mais n'a jamais été initialisée.", expr.NameToken.Loc);

        expr.Symbol = symbol;
        expr.Type = symbol.DataType;
    }

    public void Visit(FunCallExpression expr)
    {
        string functionName = expr.NameToken.Value;
        if (!Context.SymbolTable.TryGetFunction(functionName, out FunctionSymbol? symbol))
            throw new CompilerDetailedException(ErrorType.SemanticError, $"la fonction '{functionName}' n'est pas déclarée.", expr.NameToken.Loc);

        if (!symbol.IsExtern && !symbol.IsDefined)
            throw new CompilerDetailedException(ErrorType.SemanticError, $"la fonction '{functionName}' n'est pas définie.", expr.NameToken.Loc);

        if (!VerifyCallFunctionArgs(expr.Args))
            return;

        expr.Symbol = symbol;
        expr.Type = symbol.ReturnType;
    }

    private bool VerifyCallFunctionArgs(List<SyntaxExpression> args)
    {
        bool result = true;

        foreach (var param in args)
        {
            param.Accept(this);

            if (param.Type == null) 
                result = false;
        }

        return result;
    }

    public void Visit(LiteralExpression expr)
    {
        expr.Type = expr.LiteralType switch
        {
            LiteralType.Int => MarshalType.Int,
            LiteralType.String => MarshalType.String,
            LiteralType.Boolean => MarshalType.Boolean,
            LiteralType.Char => MarshalType.Char,
            _ => throw new NotImplementedException()
        };
    }

    public void Visit(ArrayInitExpression expr)
    {

    }

    private void DoVerifiedBlock(Action action)
    {
        try
        {
            action.Invoke();
        }
        catch (CompilerException exception)
        {
            if (exception is CompilerDetailedException detailedException)
                ReportDetailed(detailedException.ErrorType, detailedException.Message, detailedException.Location);
            else
                Report(exception.ErrorType, exception.Message);
        }
    }

    private static MarshalType GetWiderType(MarshalType a, MarshalType b)
    {
        if (a.SizeInBytes > b.SizeInBytes)
            return a;
        else if (a.SizeInBytes < b.SizeInBytes)
            return b;
        else
            return a;
    }

    private MarshalType ResolveType(SyntaxTypeNode node)
    {
        if (!Context.SymbolTable.HasSymbol(node.Name, SymbolType.Type))
            throw new CompilerException(ErrorType.SemanticError, $"le type '{node.Name}' n'a pas été reconnu comme étant un type valide.");

        return ResolveTypeRec(node);    
    }

    private MarshalType ResolveTypeRec(SyntaxTypeNode node)
    {
        switch (node)
        {
            case SyntaxPrimitiveType primitive:
                return Context.SymbolTable.GetType(primitive.Name);
                
            case SyntaxPointerType pointer:
                return MarshalType.CreatePointer(ResolveTypeRec(pointer.Pointee));

            case SyntaxArrayType array:
                return MarshalType.CreateArray(ResolveTypeRec(array.ElementType), array.ElementCount); 

            default:
                throw new NotImplementedException();
        }
    }
}
