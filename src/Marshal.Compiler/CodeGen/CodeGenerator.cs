using Marshal.Compiler.Errors;
using Marshal.Compiler.Semantics;
using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Statements;

namespace Marshal.Compiler.CodeGen;

public class CodeGenerator
{
    private readonly SymbolTable _symbols;
    private readonly LLVMBuilder _builder;
    private readonly ErrorHandler _errorHandler;
    private readonly TypeEvaluator _typeEvaluator;

    public CodeGenerator(SymbolTable symbols, ErrorHandler errorHandler)
    {
        _symbols = symbols;
        _errorHandler = errorHandler;
        _typeEvaluator = new TypeEvaluator(symbols, errorHandler);
        _builder = new LLVMBuilder(_typeEvaluator);
    }

    public string Generate(CompilationUnit program)
    {
        _builder.Clear();

        foreach (var statement in program.Statements)
        {
            GenerateStatement(statement);
        }

        return _builder.Build();
    }

    private void GenerateStatement(SyntaxStatement statement)
    {
        switch (statement)
        {
            case FuncDeclStatement funcDeclStmt:
                GenerateFuncDeclStatement(funcDeclStmt);
                break;
            case VarDeclStatement varDeclStmt:
                GenerateVarDeclStatement(varDeclStmt);
                break;
            case ScopeStatement scopeStmt:
                GenerateScopeStatement(scopeStmt);
                break;
            case ReturnStatement returnStmt:
                GenerateReturnStatement(returnStmt);
                break;
            case AssignmentStatement assignStmt:
                GenerateAssignmentStatement(assignStmt);
                break;
            case FunCallStatement funCallStmt:
                GenerateFunCallStatement(funCallStmt);
                break;
        }
    }

    private void GenerateScopeStatement(ScopeStatement scope)
    {
        _builder.BeginScope();

        foreach (var statement in scope.Statements)
        {
            GenerateStatement(statement);
        }

        _builder.EndScope();
    }

    private void GenerateFuncDeclStatement(FuncDeclStatement statement)
    {
        FunctionSymbol? symbol = _symbols.GetSymbol(statement.NameToken.Value, SymbolType.Function) as FunctionSymbol;
        if (symbol == null) 
        {
            _errorHandler.Report(ErrorType.Fatal, $"fonction non définie '{statement.NameToken.Value}'.");
            return;
        }

        _builder.AddFunctionSignature(symbol, statement);

        if (statement.Body != null)
        {
            GenerateScopeStatement(statement.Body);
        }
    }
    private void GenerateFunCallStatement(FunCallStatement statement)
    {
        FunctionSymbol? symbol = _symbols.GetSymbol(statement.NameIdentifier.Value, SymbolType.Function) as FunctionSymbol;
        if (symbol == null) 
        {
            _errorHandler.Report(ErrorType.Fatal, $"fonction non définie '{statement.NameIdentifier.Value}'.");
            return;
        }

        _builder.CallFunction(symbol, statement.Parameters);
    }

    private void GenerateAssignmentStatement(AssignmentStatement statement)
    {
        VariableSymbol? symbol = _symbols.GetSymbol(statement.NameIdentifier.Value, SymbolType.Variable) as VariableSymbol;
        if (symbol == null) 
        {
            _errorHandler.Report(ErrorType.Fatal, $"variable non définie '{statement.NameIdentifier.Value}'.");
            return;
        }

        TypeSymbol? type = _typeEvaluator.Evaluate(statement.AssignExpr);
        if (type == null || type == Symbol.Void)
        {
            _errorHandler.Report(ErrorType.Fatal, $"le type de l'expression n'a pas pû être identifié.");
            return;
        }

        _builder.StoreValue(symbol, statement.AssignExpr);
    }

    private void GenerateVarDeclStatement(VarDeclStatement variable)
    {
        VariableSymbol? symbol = _symbols.GetSymbol(variable.NameIdentifier.Value, SymbolType.Variable) as VariableSymbol;
        if (symbol == null) 
        {
            _errorHandler.Report(ErrorType.Fatal, $"variable non définie '{variable.NameIdentifier.Value}'.");
            return;
        }

        _builder.AllocVariable(symbol);
        if (variable.InitExpression != null)
        {
            _builder.StoreValue(symbol, variable.InitExpression);
        }
    }

    private void GenerateReturnStatement(ReturnStatement statement)
    {
        _builder.ReturnValue(statement.ReturnExpr);
    }

}