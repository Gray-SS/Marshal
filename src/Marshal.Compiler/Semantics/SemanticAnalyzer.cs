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
            Report(ErrorType.SemanticError, $"la condition de la déclaration conditionnelle ne retourne pas un booléen.");

        scope.Scope.Accept(this);
    }

    public void Visit(WhileStatement stmt)
    {
        stmt.CondExpr.Accept(this);
        if (stmt.CondExpr.Type != MarshalType.Boolean)
            Report(ErrorType.SemanticError, $"la condition de la déclaration conditionnelle ne retourne pas un booléen.");

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
                    ReportDetailed(ErrorType.Warning, $"la déclaration de retour n'est jamais atteinte.", statement.ReturnKeyword.Loc);

                if (!IsTypeCompatible(statement.ReturnExpr.Type, function.ReturnType))
                    ReportDetailed(ErrorType.Error, $"la déclaration de retour attend une expression de type '{function.ReturnType.Name}' mais une expression de type '{statement.ReturnExpr.Type.Name}' a été reçue.", statement.ReturnKeyword.Loc);
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
                ReportDetailed(ErrorType.SemanticError, $"impossible d'assigner une valeur de type '{stmt.Initializer.Type.Name}' à '{stmt.Symbol.DataType.Name}'.", stmt.NameToken.Loc);
                return;
            }
        }
    }

    public void Visit(AssignmentStatement stmt)
    {
        stmt.Initializer.Accept(this);

        if (!IsTypeCompatible(stmt.Initializer.Type, stmt.Symbol.DataType))
        {
            ReportDetailed(ErrorType.SemanticError, $"impossible d'assigner une valeur de type '{stmt.Initializer.Type.Name}' à '{stmt.Symbol.DataType.Name}'.", stmt.NameToken.Loc);
            return;
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

    public void Visit(BinaryOpExpression expr)
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
            var stmtParam = args[i];
            var functionParam = function.Params[i];

            if (!IsTypeCompatible(functionParam.DataType, stmtParam.Type))
            {
                ReportDetailed(ErrorType.SemanticError, $"l'argument '{functionParam.Name}' de la fonction '{function.Name}' attendait une expression de type '{functionParam.DataType.Name}' mais un argument de type '{stmtParam.Type.Name}' a été reçu.", functionLoc);
                continue;
            }
        }
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