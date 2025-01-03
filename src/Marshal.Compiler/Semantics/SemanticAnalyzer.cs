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

    public void Visit(StructDeclStatement stmt)
    {
        
    }

    public void Visit(FieldDeclStatement stmt)
    {

    }

    public void Visit(FuncDeclStatement stmt)
    {
        FunctionSymbol function = stmt.Symbol;
        
        if (stmt.Body != null)
        {
            stmt.Body.Accept(this);
            var returnStatements = stmt.Body.Statements.OfType<ReturnStatement>();
            foreach (ReturnStatement statement in returnStatements)
            {
                if (statement != returnStatements.First())
                    ReportDetailed(ErrorType.Warning, $"la déclaration de retour n'est jamais atteinte.", statement.Loc);

                if (function.ReturnType != MarshalType.Void)
                {
                    if (statement.ReturnExpr == null)
                        ReportDetailed(ErrorType.SemanticError, $"la déclaration de retour attend une expression de type '{function.ReturnType.Name}' mais aucune valeur n'a été retournée.", statement.Loc);
                    else if (!IsTypeCompatible(statement.ReturnExpr.Type, function.ReturnType))
                        ReportDetailed(ErrorType.SemanticError, $"le retour d'une valeur de type '{statement.ReturnExpr.Type.Name}' n'est pas directement possible '{function.ReturnType.Name}'. Un cast explicite peut-être nécessaire.", statement.ReturnExpr.Loc);
                }
                else
                {
                    if (statement.ReturnExpr != null)
                        ReportDetailed(ErrorType.SemanticError, $"la déclaration de retour n'attend aucune valeur mais une expression de type '{statement.ReturnExpr.Type.Name}' a été retournée.", statement.ReturnExpr.Loc);
                }
            }
        }
    }

    public void Visit(VarDeclStatement stmt)
    {
        if (stmt.Initializer != null)
        {
            stmt.Initializer.Accept(this);
            
            if (!IsTypeCompatible(stmt.Initializer.Type, stmt.Symbol.DataType))
            {
                ReportDetailed(ErrorType.SemanticError, $"l'assignement d'une valeur de type '{stmt.Initializer.Type.Name}' à '{stmt.Symbol.DataType.Name}' n'est pas directement possible. Un cast explicite est peut-être nécéssaire.", stmt.Initializer.Loc);
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

        if (!IsTypeCompatible(stmt.Initializer.Type, stmt.LExpr.Type))
        {
            ReportDetailed(ErrorType.SemanticError, $"l'assignement d'une valeur de type '{stmt.Initializer.Type.Name}' à '{stmt.LExpr.Type.Name}' n'est pas directement possible. Un cast explicite est peut-être nécéssaire.", stmt.Initializer.Loc);
        }
    }

    public void Visit(IncrementStatement stmt)
    {
        if (!CanIncrement(stmt.Symbol.DataType))
        {
            ReportDetailed(ErrorType.SemanticError, $"impossible d'incrémenter une variable de type '{stmt.Symbol.DataType.Name}'.", stmt.Loc);
        }
    }

    private static bool CanIncrement(MarshalType type)
    {
        if (type.IsNumeric) return true;
        if (type.IsPointer) return true;

        return false;
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
        CastKind castRes = expr.CastedExpr.Type.GetCastKind(expr.Type);

        if (castRes == CastKind.Invalid)
            ReportDetailed(ErrorType.SemanticError, $"impossible de cast la valeur '{expr.CastedExpr.Type.Name}' en '{expr.Type}'.", expr.Loc);

        if (castRes == CastKind.Implicit)
            ReportDetailed(ErrorType.Warning, $"casting non nécessaire entre '{expr.Type.Name}' et '{expr.CastedExpr.Type.Name}'.", expr.Loc);
    }

    public void Visit(FunCallExpression expr)
    {
        FunctionSymbol function = expr.Symbol;

        AnalyzeFunctionCall(function, expr.Args, expr.NameToken.Loc);

        if (function.ReturnType == MarshalType.Void)
            ReportDetailed(ErrorType.SemanticError, $"impossible d'assigner une valeur à la fonction '{function.Name}' car elle n'attend rien en retour.", expr.Loc);
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

    public void Visit(MemberAccessExpression expr)
    {
    }

    public void Visit(ArrayAccessExpression expr)
    {
    }

    public void Visit(ArrayInitExpression expr)
    {
    }

    private bool IsTypeCompatible(MarshalType source, MarshalType dest)
    {
        CastKind result = source.GetCastKind(dest);
        return result == CastKind.Implicit;
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

            CastKind castRes = paramSymbol.DataType.GetCastKind(paramExpr.Type);
            if (castRes != CastKind.Implicit)
            {
                ReportDetailed(ErrorType.SemanticError, $"l'assignement d'une valeur de type '{paramExpr.Type.Name}' au type '{paramSymbol.DataType.Name}' n'est pas possible directement. Un cast explicite peut-être nécessaire.", paramExpr.Loc);
                continue;
            }
        }
    }
}