using Marshal.Compiler.Errors;
using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;

namespace Marshal.Compiler.Semantics;

public class SemanticAnalyzer : CompilerPass, IASTVisitor
{
    public SemanticAnalyzer(CompilationContext context, ErrorHandler errorHandler) : base(context, errorHandler)
    {
    }

    public override void Apply()
    {
        Visit(Context.AST);
    }

    public void Visit(CompilationUnit unit)
    {
        foreach (SyntaxStatement statement in unit.Statements)
        {
            statement.Accept(this);
        }
    }

    public void Visit(IfStatement stmt)
    {
        foreach (ConditionalScope item in stmt.IfsScopes)
            VerifyConditionalScope(item);

        stmt.ElseScope?.Accept(this);   
    }

    private void VerifyConditionalScope(ConditionalScope scope)
    {
        scope.ConditionExpr.Accept(this);
        if (scope.ConditionExpr.Type != MarshalType.Boolean)
            ReportDetailed(ErrorType.SemanticError, $"la condition de la déclaration conditionnelle ne retourne pas un booléen.", scope.ConditionExpr.Loc);

        scope.Scope.Accept(this);
    }

    public void Visit(WhileStatement stmt)
    {
        stmt.CondExpr.Accept(this);
        if (stmt.CondExpr.Type != MarshalType.Boolean)
            ReportDetailed(ErrorType.SemanticError, $"la condition de la déclaration conditionnelle ne retourne pas un booléen.", stmt.CondExpr.Loc);

        stmt.Scope.Accept(this);
    }

    public void Visit(ScopeStatement stmt)
    {
        foreach (SyntaxStatement statement in stmt.Statements)
        {
            statement.Accept(this);
        }
    }

    public void Visit(FuncDeclStatement stmt)
    {
        FunctionSymbol function = stmt.Symbol;

        if (function.ReturnType != MarshalType.Void && stmt.Body != null)
        {
            var returnStatements = stmt.Body.Statements.OfType<ReturnStatement>();
            foreach (ReturnStatement statement in returnStatements)
            {
                if (statement != returnStatements.First())
                    ReportDetailed(ErrorType.Warning, $"la déclaration de retour n'est jamais atteinte.", statement.Loc);

                if (!IsTypeCompatible(statement.ReturnExpr.Type, function.ReturnType))
                    ReportDetailed(ErrorType.Error, $"la déclaration de retour attend une expression de type '{function.ReturnType.Name}' mais une expression de type '{statement.ReturnExpr.Type.Name}' a été reçue.", statement.ReturnExpr.Loc);
            }
        }
        
        stmt.Body?.Accept(this);
    }

    public void Visit(VarDeclStatement stmt)
    {
        if (stmt.Initializer != null)
        {
            stmt.Initializer.Accept(this);
            
            if (!IsTypeCompatible(stmt.Initializer.Type, stmt.Symbol.DataType))
            {
                ReportDetailed(ErrorType.SemanticError, $"impossible d'assigner une valeur de type '{stmt.Initializer.Type.Name}' à '{stmt.Symbol.DataType.Name}'.", stmt.Initializer.Loc);
                return;
            }
        }
    }

    public void Visit(AssignmentStatement stmt)
    {
        stmt.LExpr.Accept(this);
        stmt.Initializer.Accept(this);

        if (stmt.LExpr.ValueCategory != ValueCategory.Locator)
        {
            ReportDetailed(ErrorType.SemanticError, $"l'expression à gauche doit être une expression de type locator (pointeur, déférencement de pointeur, accès à un tableau, ect...)", stmt.LExpr.Loc);
        }

        if (!IsTypeCompatible(stmt.Initializer.Type, stmt.Symbol.DataType))
        {
            ReportDetailed(ErrorType.SemanticError, $"impossible d'assigner une valeur de type '{stmt.Initializer.Type.Name}' à '{stmt.Symbol.DataType.Name}'.", stmt.Initializer.Loc);
        }
    }

    public void Visit(IncrementStatement stmt)
    {
        if (!stmt.Symbol.DataType.IsNumeric) 
        {
            ReportDetailed(ErrorType.SemanticError, $"impossible d'incrémenter une variable non numérique '{stmt.Symbol.Name}'", stmt.Loc);
        }
    }

    public void Visit(FunCallStatement stmt)
    {
        FunctionSymbol function = stmt.Symbol;

        AnalyzeFunctionCall(function, stmt.Args, stmt.NameToken.Loc);
    }

    public void Visit(ReturnStatement stmt)
    {
    }

    public void Visit(CastExpression expr)
    {
        if (expr.Type == expr.CastedExpr.Type)
            ReportDetailed(ErrorType.Warning, $"casting non nécessaire entre '{expr.Type.Name}' et '{expr.CastedExpr.Type.Name}'.", expr.Loc);

        if (!CanExplicitlyCast(expr.CastedExpr.Type, expr.Type))
            ReportDetailed(ErrorType.Error, $"impossible de cast une expression de type '{expr.CastedExpr.Type.Name}' en '{expr.Type.Name}'", expr.Loc);
    }

    public void Visit(FunCallExpression expr)
    {
        FunctionSymbol function = expr.Symbol;

        AnalyzeFunctionCall(function, expr.Args, expr.NameToken.Loc);
    }


    public void Visit(NewExpression expr)
    {
    }

    public void Visit(LiteralExpression expr)
    {
    }

    public void Visit(BracketExpression expr)
    {
    }

    public void Visit(BinaryOpExpression expr)
    {
    }

    public void Visit(UnaryOpExpression expr)
    {
    }

    public void Visit(VarRefExpression expr)
    {
    }

    public void Visit(ArrayAccessExpression expr)
    {
    }

    public void Visit(ArrayInitExpression expr)
    {
    }

    private void AnalyzeFunctionCall(FunctionSymbol function, List<SyntaxExpression> args, Location functionLoc)
    {
        if (function.Params.Count != args.Count)
        {
            ReportDetailed(ErrorType.SemanticError, $"la fonction '{function.Name}' attendait {function.Params.Count} arguments mais {args.Count} ont été reçu.", functionLoc);
            return;
        }

        for (int i = 0; i < function.Params.Count; i++)
        {
            SyntaxExpression paramExpr = args[i];
            VariableSymbol paramSymbol = function.Params[i];

            if (!IsTypeCompatible(paramSymbol.DataType, paramExpr.Type))
            {
                ReportDetailed(ErrorType.SemanticError, $"l'argument '{paramSymbol.Name}' de la fonction '{function.Name}' attendait une expression de type '{paramSymbol.DataType.Name}' mais un argument de type '{paramExpr.Type.Name}' a été reçu.", paramExpr.Loc);
                continue;
            }
        }
    }

    private static bool CanExplicitlyCast(MarshalType from, MarshalType to)
    {
        if (from.IsNumeric && to.IsNumeric)
            return true;

        if (from is PointerType && to.IsNumeric && ((PointerType)from).Pointee.IsNumeric)
            return true;

        if (from is PointerType && to is PointerType && ((PointerType)from).Pointee.IsNumeric && ((PointerType)to).Pointee.IsNumeric)
            return true;

        return false;
    }

    private static bool IsTypeCompatible(MarshalType left, MarshalType right)
    {
        if (left.Base.IsNumeric && right.Base.IsNumeric)
            return true;

        if (left.Base.IsBoolean && right.Base.IsBoolean)
            return true;

        if (left is PointerType leftPointer && right is PointerType rightPointer)
        {
            return IsTypeCompatible(leftPointer.Pointee, rightPointer.Pointee);
        }

        if (left is ArrayType leftArray && right is ArrayType rightArray)
        {
            return IsTypeCompatible(leftArray.ElementType, rightArray.ElementType);
        }

        if (left is TypeAlias lalias)
        {
            return IsTypeCompatible(lalias.Aliased, right);
        }

        if (right is TypeAlias ralias)
        {
            return IsTypeCompatible(left, ralias.Aliased);
        }

        if (left.Base.Name == right.Base.Name)
            return true;

        if (left.Base == MarshalType.Void || right.Base == MarshalType.Void)
            return false;

        if (left.Base == right.Base)
            return true;

        return false;
    }
}