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

    public void Visit(CompilationUnit unit) => DoVerifiedBlock(() =>
    {
        foreach (SyntaxStatement statement in unit.Statements)
        {
            statement.Accept(this);
        }
    });

    public void Visit(ScopeStatement stmt)
    {
        Context.SymbolTable.EnterScope();

        DoVerifiedBlock(() => 
        {
            foreach (SyntaxStatement statement in stmt.Statements)
            {
                statement.Accept(this);
            }
        });

        Context.SymbolTable.ExitScope();
    }

    public void Visit(IfStatement stmt)
    {
        foreach (ConditionalScope item in stmt.IfsScopes)
        {
            item.ConditionExpr.Accept(this);
            item.Scope.Accept(this);
        }

        stmt.ElseScope?.Accept(this);
    }

    public void Visit(WhileStatement stmt)
    {
        stmt.CondExpr.Accept(this);
        stmt.Scope.Accept(this);
    }

    public void Visit(StructDeclStatement stmt)
    {
        string typeName = stmt.Identifier.Value;

        if (Context.SymbolTable.TryGetType(typeName, out MarshalType? type))
        {
            if (type is StructType)
                throw new CompilerDetailedException(ErrorType.SemanticError, $"Un type avec le même nom a déjà été déclaré.", stmt.Loc);
            else
                throw new CompilerDetailedException(ErrorType.SemanticError, $"Impossible de nommer le type '{typeName}' car un type système porte déjà ce nom.", stmt.Loc);
        }

        Context.SymbolTable.EnterScope();

        var fields = stmt.Fields.Select(x => {
            x.Accept(this);
            return x.Symbol!;
        }).ToArray();

        Context.SymbolTable.ExitScope();

        int sizeInBytes = fields.Sum(x => {
            return x.DataType.SizeInBytes;
        });

        var symbol = new StructType(typeName, sizeInBytes, fields);
        Context.SymbolTable.AddSymbol(symbol);

        stmt.Symbol = symbol;
    }

    public void Visit(FieldDeclStatement stmt)
    {
        FieldSymbol? field = Context.SymbolTable.GetSymbol(stmt.FieldName, SymbolType.Field) as FieldSymbol;
        if (field != null) 
            throw new CompilerDetailedException(ErrorType.SemanticError, $"le champ '{stmt.FieldName}' est déjà déclaré dans la structure.", stmt.Loc);

        field = new FieldSymbol(stmt.FieldName, ResolveType(stmt.SyntaxType));
        Context.SymbolTable.AddSymbol(field);

        stmt.Symbol = field;
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
        stmt.LExpr.Accept(this);
        stmt.Initializer.Accept(this);
    }

    public void Visit(IncrementStatement stmt)
    {
        // TODO: Move this logic to the Semantic Analysis phase.

        // string variableName = stmt.NameToken.Value;
        // if (!Context.SymbolTable.TryGetVariable(variableName, out VariableSymbol? symbol))
        //     throw new CompilerDetailedException(ErrorType.SemanticError, $"la variable '{variableName}' n'existe pas dans le contexte actuel.", stmt.NameToken.Loc);

        // stmt.Symbol = symbol;
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
        stmt.ReturnExpr?.Accept(this);
    }

    public void Visit(CastExpression expr)
    {
        var type = ResolveType(expr.CastedType);
        expr.CastedExpr.Accept(this);

        expr.Type = type;
    }

    public void Visit(UnaryOpExpression expr)
    {
        expr.Operand.Accept(this);

        if (expr.Operation.IsNumericOperation())
        {
            if (!expr.Operand.Type.IsNumeric)
                throw new CompilerException(ErrorType.SemanticError, $"opération invalide '{expr.Operation}' pour l'expression de type non numérique '{expr.Operand.Type.Name}'.");

            expr.Type = expr.Operand.Type;
        }
        else if (expr.Operation.IsComparisonOperation())
        {
            if (expr.Operand.Type != MarshalType.Boolean)
                throw new CompilerDetailedException(ErrorType.SemanticError, $"opération invalide '{expr.Operation}' pour l'expression de type non booléenne '{expr.Operand.Type.Name}'.", expr.Operand.Loc);
        
            expr.Type = MarshalType.Boolean;
        }
        else if (expr.Operation == UnaryOpType.AddressOf)
        {
            if (expr.Operand.ValueCategory == ValueCategory.Transient)
                throw new CompilerDetailedException(ErrorType.SemanticError, $"impossible de récupérer l'addresse d'une valeur transient.", expr.Operand.Loc);

            expr.Type = MarshalType.CreatePointer(expr.Operand.Type);
        }
        else if (expr.Operation == UnaryOpType.Deference)
        {
            MarshalType operandType = expr.Operand.Type;
            if (!operandType.IsPointer)
                throw new CompilerDetailedException(ErrorType.SemanticError, $"déférencement invalide pour le type '{operandType.Name}' car le type déférencé doit être un pointeur.", expr.Operand.Loc);

            MarshalType type;
            if (operandType is PointerType pointer)
                type = pointer.Pointee;
            else if (operandType is ArrayType array)
                type = array.ElementType;
            else
                throw new NotImplementedException($"Deferencement for type '{operandType.Name}' is not supported.");

            expr.Type = type;
        }
        else throw new NotImplementedException($"Unary operation not implemented '{expr.Operation}'.");
    }

    public void Visit(BracketExpression expr)
    {
        expr.Expression.Accept(this);
        expr.Type = expr.Expression.Type;
    }

    public void Visit(BinaryOpExpression expr)
    {
        expr.Left.Accept(this);
        expr.Right.Accept(this);

        if (expr.Operation.IsNumericOperation())
        {
            if (!IsNumericOperationPossible(expr.Left.Type, expr.Right.Type, expr.Operation))
                throw new CompilerException(ErrorType.SemanticError, $"opération numérique invalide '{expr.Operation}' entre valeurs de type '{expr.Left.Type.Name}' et '{expr.Right.Type.Name}'.");

            expr.Type = GetWiderType(expr.Left.Type, expr.Right.Type);
        }
        else if (expr.Operation.IsComparisonOperation())
        {
            if (!AreComparableTypes(expr.Left.Type, expr.Right.Type))
                throw new CompilerException(ErrorType.SemanticError, $"comparaison immpossible entre valeurs de type '{expr.Left.Type.Name}' et '{expr.Right.Type.Name}'.");

            expr.Type = MarshalType.Boolean;
        }
        else throw new NotImplementedException();
    }

    public void Visit(VarRefExpression expr)
    {
        string variableName = expr.NameToken.Value;

        if (!Context.SymbolTable.TryGetVariable(variableName, out VariableSymbol? symbol))
            throw new CompilerDetailedException(ErrorType.SemanticError, $"la variable '{variableName}' n'est pas déclarée.", expr.NameToken.Loc);

        if (!symbol.IsInitialized && !symbol.DataType.IsArray && symbol.DataType is not StructType)
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

    public void Visit(MemberAccessExpression expr)
    {
        expr.VarExpr.Accept(this);

        if (expr.VarExpr.Type is not StructType structType)
            throw new CompilerDetailedException(ErrorType.SemanticError, $"vous essayez d'accéder à un membre d'un type qui n'est pas une structure. ({expr.VarExpr.Type.Name})", expr.Loc);
        
        if (!structType.TryGetField(expr.MemberName, out FieldSymbol? field))
            throw new CompilerDetailedException(ErrorType.SemanticError, $"le champ '{expr.MemberName}' n'existe pas dans la structure '{structType.Name}'.", expr.Loc);
        
        expr.Type = field.DataType;
        expr.MemberIdx = structType.GetFieldIndex(expr.MemberName);
    }

    public void Visit(ArrayAccessExpression expr)
    {
        expr.ArrayExpr.Accept(this);
        expr.IndexExpr.Accept(this);

        if (!expr.ArrayExpr.Type.IsIndexable)
            throw new CompilerException(ErrorType.SemanticError, "vous essayez d'indexer un type qui n'est pas indexable.");

        if (expr.IndexExpr.Type != MarshalType.Int)
            throw new CompilerException(ErrorType.SemanticError, $"l'index doit obligatoirement être de type '{MarshalType.Int.Name}' mais un type '{expr.IndexExpr.Type.Name}' a été reçu.");

        if (expr.ArrayExpr.Type is ArrayType arrayType)
            expr.Type = arrayType.ElementType;
        else if (expr.ArrayExpr.Type is PointerType pointerType)
            expr.Type = pointerType.Pointee;
        else if (expr.ArrayExpr.Type is StringType)
            expr.Type = MarshalType.Char;
        else
            throw new NotImplementedException();
    }

    public void Visit(NewExpression expr)
    {
        if (expr is NewArrayExpression arrayExpr)
        {
            arrayExpr.LengthExpr.Accept(this);

            var type = ResolveType(expr.TypeName.Value);
            if (arrayExpr.LengthExpr.Type != MarshalType.Int)
                throw new CompilerException(ErrorType.SemanticError, $"la taille du tableau doit obligatoirement être de type '{MarshalType.Int.Name}' mais un type '{arrayExpr.LengthExpr.Type.Name}' a été reçu.");

            arrayExpr.Type = MarshalType.CreateDynamicArray(type);
        }
        else if (expr is NewStructExpression typeExpr)
        {
            typeExpr.Arguments.ForEach(x => x.Accept(this));

            var type = ResolveType(expr.TypeName.Value);
            typeExpr.Type = type;
        }
    }

    public void Visit(ArrayInitExpression expr)
    {
        throw new NotSupportedException("Array init expressions aren't supported anymore.");

        // var initializers = expr.Expressions;

        // MarshalType type = null!;
        // foreach (SyntaxExpression item in initializers)
        // {
        //     item.Accept(this);
        //     if (type == null) type = item.Type;
        //     else if (item.Type != type)
        //         throw new CompilerException(ErrorType.SemanticError, "toutes les expressions du tableau doivent être du même type.");
        // }

        // expr.Type = MarshalType.Crea(type, initializers.Count);
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
        if (a.SizeInBytes >= b.SizeInBytes) return a;
        else return b;
    }

    private static bool AreComparableTypes(MarshalType leftType, MarshalType rightType)
    {
        return (leftType.IsNumeric && rightType.IsNumeric) || (leftType == MarshalType.Boolean && rightType == MarshalType.Boolean);
    }

    private static bool IsNumericOperationPossible(MarshalType leftType, MarshalType rightType, BinOpType operation)
    {
        if (leftType.IsNumeric && rightType.IsNumeric)
            return true;

        if (leftType.IsPointer && rightType.IsNumeric && (operation == BinOpType.Addition || operation == BinOpType.Subtraction))
            return true;

        if (leftType.IsNumeric && rightType.IsPointer && (operation == BinOpType.Addition))
            return true;

        return false;
    }

    private MarshalType ResolveType(string name)
    {
        if (!Context.SymbolTable.TryGetType(name, out MarshalType? type))
            throw new CompilerException(ErrorType.SemanticError, $"le type '{name}' n'a pas été reconnu comme étant un type valide.");

        if (type is TypeAlias alias)
            return alias.Aliased;

        return type;
    }

    private MarshalType ResolveType(SyntaxTypeNode node)
    {
        if (!Context.SymbolTable.HasSymbol(node.BaseName, SymbolType.Type))
            throw new CompilerException(ErrorType.SemanticError, $"le type '{node.BaseName}' n'a pas été reconnu comme étant un type valide.");

        return ResolveTypeRec(node);    
    }

    private MarshalType ResolveTypeRec(SyntaxTypeNode node)
    {
        switch (node)
        {
            case SyntaxPrimitiveType primitive:
                var type = Context.SymbolTable.GetType(primitive.Name);

                if (type is TypeAlias alias)
                    return alias.Aliased;
                
                return type;
                
            case SyntaxPointerType pointer:
                return MarshalType.CreatePointer(ResolveTypeRec(pointer.Pointee));

            case SyntaxArrayType array:
                return MarshalType.CreateDynamicArray(ResolveTypeRec(array.ElementType)); 

            default:
                throw new NotImplementedException();
        }
    }
}
