using Marshal.Compiler.Errors;
using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;

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
        foreach (SyntaxStatement statement in unit.Statements)
        {
            statement.Accept(this);
        }
    }

    public void Visit(ScopeStatement stmt)
    {
        Context.SymbolTable.EnterScope();

        foreach (SyntaxStatement statement in stmt.Statements)
        {
            statement.Accept(this);
        }

        Context.SymbolTable.ExitScope();
    }

    public void Visit(FuncDeclStatement stmt)
    {
        string functionName = stmt.NameToken.Value;
        if (Context.SymbolTable.HasSymbol(functionName, SymbolType.Function))
        {
            ReportDetailed(ErrorType.SemanticError, $"une fonction nommée '{functionName}' existe déjà dans le contexte actuel.", stmt.NameToken.Loc);
            return;
        }

        var args = new List<VariableSymbol>();
        for (int i = 0; i < stmt.Params.Count; i++)
        {
            var param = stmt.Params[i];

            var arg = new VariableSymbol(param.NameToken.Value, true);
            args.Add(arg);
        }

        bool isDefined = stmt.Body != null;

        var function = new FunctionSymbol(functionName, stmt.IsExtern, isDefined, args);
        Context.SymbolTable.AddSymbol(function); 

        if (stmt.Body != null)
        {
            Context.SymbolTable.EnterScope();

            foreach (VariableSymbol arg in args)
            {
                Context.SymbolTable.AddSymbol(arg);
            }

            stmt.Body.Accept(this);

            Context.SymbolTable.ExitScope();
        }

        stmt.Symbol = function;
    }
    
    public void Visit(VarDeclStatement stmt)
    {
        string variableName = stmt.NameToken.Value;
        if (Context.SymbolTable.HasSymbol(variableName, SymbolType.Variable))
        {
            ReportDetailed(ErrorType.SemanticError, $"une variable nommée '{variableName}' existe déjà dans le contexte actuel.", stmt.NameToken.Loc);
            return;
        }

        bool isInitialized = stmt.Initializer != null;

        var variable = new VariableSymbol(variableName, isInitialized);
        Context.SymbolTable.AddSymbol(variable);

        stmt.Initializer?.Accept(this);

        stmt.Symbol = variable;
    }


    public void Visit(AssignmentStatement stmt)
    {
        string variableName = stmt.NameToken.Value;
        if (!Context.SymbolTable.TryGetVariable(variableName, out VariableSymbol? symbol))
        {
            ReportDetailed(ErrorType.SemanticError, $"une variable nommée '{variableName}' existe déjà dans le contexte actuel.", stmt.NameToken.Loc);
            return;
        }

        stmt.AssignExpr?.Accept(this);

        stmt.Symbol = symbol;
    }

    public void Visit(VarRefExpression expr)
    {
        string variableName = expr.NameToken.Value;

        if (!Context.SymbolTable.TryGetVariable(variableName, out VariableSymbol? symbol))
        {
            ReportDetailed(ErrorType.SemanticError, $"la variable '{variableName}' n'est pas déclarée.", expr.NameToken.Loc);
            return;
        }

        expr.Symbol = symbol;
    }

    public void Visit(FunCallExpression expr)
    {
        string functionName = expr.NameToken.Value;
        if (!Context.SymbolTable.TryGetFunction(functionName, out FunctionSymbol? symbol))
        {
            ReportDetailed(ErrorType.SemanticError, $"la fonction '{functionName}' n'est pas déclarée.", expr.NameToken.Loc);
            return;
        }

        foreach (var param in expr.Parameters)
        {
            param.Accept(this);
        }

        expr.Symbol = symbol;
    }

    public void Visit(FunCallStatement stmt)
    {
        string functionName = stmt.NameToken.Value;
        if (!Context.SymbolTable.TryGetFunction(functionName, out FunctionSymbol? symbol))
        {
            ReportDetailed(ErrorType.SemanticError, $"la fonction '{functionName}' n'est pas déclarée.", stmt.NameToken.Loc);
            return;
        }

        foreach (var param in stmt.Parameters)
        {
            param.Accept(this);
        }

        stmt.Symbol = symbol;
    }

    public void Visit(ReturnStatement stmt)
    {
        stmt.ReturnExpr.Accept(this);
    }


    public void Visit(BinaryOpExpression stmt)
    {
        stmt.Left.Accept(this);
        stmt.Right.Accept(this);
    }

    public void Visit(LiteralExpression expr)
    {
    }

    public void Visit(ArrayInitExpression expr)
    {
    }
}
