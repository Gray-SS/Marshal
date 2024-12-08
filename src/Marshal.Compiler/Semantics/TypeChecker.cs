using Marshal.Compiler.Errors;
using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;
using Marshal.Compiler.Types;

namespace Marshal.Compiler.Semantics;

public class TypeChecker : CompilerPass, IASTVisitor
{
    public TypeChecker(CompilationContext context, ErrorHandler errorHandler) : base(context, errorHandler)
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

    public void Visit(ScopeStatement stmt)
    {
        foreach (SyntaxStatement statement in stmt.Statements)
        {
            statement.Accept(this);
        }
    }

    public void Visit(FuncDeclStatement stmt)
    {
        if (!RequireType(stmt.ReturnType, stmt.NameToken.Loc))
            return;

        stmt.Symbol.ReturnType = stmt.ReturnType;

        stmt.Body?.Accept(this);
    }

    public void Visit(VarDeclStatement stmt)
    {
        if (!RequireType(stmt.VarType, stmt.NameToken.Loc))
            return;

        stmt.Symbol.DataType = stmt.VarType;
        
        if (stmt.Initializer != null)
        {
            stmt.Initializer.Accept(this);
            
            if (stmt.VarType != stmt.Initializer.Type)
            {
                ReportDetailed(ErrorType.SemanticError, $"impossible d'assigner une valeur de type '{stmt.Initializer.Type.Name}' à '{stmt.VarType.Name}'.", stmt.NameToken.Loc);
                return;
            }
        }
    }

    public void Visit(AssignmentStatement stmt)
    {
    }

    public void Visit(FunCallStatement stmt)
    {
    }

    public void Visit(BinaryOpExpression stmt)
    {
    }

    public void Visit(ReturnStatement stmt)
    {

    }

    public void Visit(FunCallExpression expr)
    {
        expr.Type = expr.Symbol.ReturnType;
    }

    public void Visit(LiteralExpression expr)
    {
        switch (expr.LiteralType)
        {
            case LiteralType.Int:
                expr.Type = MarshalType.Int;    
                break;
            case LiteralType.String:
                expr.Type = MarshalType.String;
                break;
            default:
                Report(ErrorType.InternalError, $"l'expression litérale de type '{expr.LiteralType}' n'est pas prise en charge dans le type checking.");
                break;
        }
    }

    public void Visit(VarRefExpression expr)
    {
        expr.Type = expr.Symbol.DataType;
    }

    public void Visit(ArrayInitExpression expr)
    {
        
    }

    private bool RequireType(MarshalType type, Location loc)
    {
        if (!Context.SymbolTable.HasSymbol(type.Primitive.Name, SymbolType.Type))
        {
            ReportDetailed(ErrorType.SemanticError,  $"le type '{type.Primitive.Name}' n'est pas reconnu comme étant un type valide.", loc);
            return false;
        }

        return true;
    }
}