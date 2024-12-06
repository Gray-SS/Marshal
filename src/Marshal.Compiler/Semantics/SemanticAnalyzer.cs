using Marshal.Compiler.Errors;
using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;

namespace Marshal.Compiler.Semantics;

public class SemanticAnalyzer
{
    private readonly SymbolTable _symbols;
    private readonly ErrorHandler _errorHandler;
    private readonly TypeEvaluator _typeEvaluator;

    public SemanticAnalyzer(SymbolTable symbolTable, ErrorHandler errorHandler)
    {
        _symbols = symbolTable;
        _errorHandler = errorHandler;
        _typeEvaluator = new TypeEvaluator(symbolTable, errorHandler);
    }

    public void Analyze(CompilationUnit program)
    {
        foreach (SyntaxStatement statement in program.Statements)
        {
            AnalyzeStatement(statement);
        }
    }

    private void AnalyzeExpression(SyntaxExpression expr)
    {
        switch (expr)
        {
            case FunCallExpression funCallExpr:
                AnalyzeFunCallExpression(funCallExpr);
                break;
            case VarRefExpression varRefExpr:
                AnalyzeVarRefExpression(varRefExpr);
                break;
        }
    }

    private void AnalyzeVarRefExpression(VarRefExpression expr)
    {
        string varName = expr.NameIdentifier.Value;
        if (!_symbols.HasSymbol(varName, SymbolType.Variable))
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"la variable '{varName}' n'est pas définie.", expr.NameIdentifier.Location);
    }

    private void AnalyzeFunCallExpression(FunCallExpression expr)
    {
        string functionName = expr.NameIdentifier.Value;
        if (!_symbols.TryGetFunction(functionName, out FunctionSymbol? funSymbol))
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"impossible d'appeler la fonction '{functionName}' car elle n'existe pas dans le context actuel.", expr.NameIdentifier.Location);

        if (funSymbol != null)
        {
            if (expr.Parameters.Count != funSymbol.Params.Count)
            {
                _errorHandler.ReportDetailed(ErrorType.SemanticError, $"la fontion '{functionName}' attend {funSymbol.Params.Count} paramètre(s) mais '{expr.Parameters.Count}' ont été fourni.", expr.NameIdentifier.Location);
            }
            else
            {
                for (int i = 0; i < expr.Parameters.Count; i++)
                {
                    SyntaxExpression exprParam = expr.Parameters[i];

                    AnalyzeExpression(exprParam);
                    TypeSymbol exprType = _typeEvaluator.Evaluate(exprParam); 
                    ParamSymbol funParam = funSymbol.Params[i];
                    
                    if (exprType != Symbol.Void && funParam.Type != exprType)
                    {
                        _errorHandler.ReportDetailed(ErrorType.SemanticError, $"impossible de convertir '{exprType.Name}' en '{funParam.Type.Name}'.", expr.NameIdentifier.Location);
                    }
                }
            }
        }
    }

    private void AnalyzeStatement(SyntaxStatement statement)
    {
        switch (statement)
        {
            case FuncDeclStatement funcDeclStmt:
                AnalyzeFuncDeclStatement(funcDeclStmt);
                break;
            case VarDeclStatement varDeclStmt:
                AnalyzeVarDeclStatement(varDeclStmt);
                break;
            case AssignmentStatement assignStmt:
                AnalyzeAssignementStatement(assignStmt);
                break;
            case ScopeStatement scopeStmt:
                AnalyzeScopeStatement(scopeStmt);
                break;
        }
    }

    private void AnalyzeVarDeclStatement(VarDeclStatement statement)
    {
        string varName = statement.NameIdentifier.Value;
        if (_symbols.HasSymbol(varName, SymbolType.Variable))
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"une variable avec le nom '{varName}' existe déjà dans ce contexte.", statement.NameIdentifier.Location);

        string typeName = statement.TypeIdentifier.Value;
        if (!_symbols.TryGetType(typeName, out TypeSymbol? varType))
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"le type '{typeName}' n'est pas reconnu comme étant un type valide.", statement.TypeIdentifier.Location);

        if (varType != null && varType == Symbol.Void)
        {
            _errorHandler.Report(ErrorType.SemanticError, $"le type d'une variable ne peut pas être void.");
        }

        if (varType != null && statement.InitExpression != null)
        {
            AnalyzeExpression(statement.InitExpression);
            TypeSymbol initExprType = _typeEvaluator.Evaluate(statement.InitExpression);

            if (initExprType != Symbol.Void && initExprType != varType)
                _errorHandler.ReportDetailed(ErrorType.SemanticError, $"impossible de convertir le type '{initExprType.Name}' en '{varType.Name}'.", statement.NameIdentifier.Location);
        }

        var variable = new VariableSymbol(varName, varType!, true);
        _symbols.AddSymbol(variable);
    }

    private void AnalyzeAssignementStatement(AssignmentStatement statement)
    {
        string varName = statement.NameIdentifier.Value;
        if (!_symbols.TryGetVariable(varName, out VariableSymbol? variable))
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"la variable '{varName}' n'existe pas dans le contexte actuel.", statement.NameIdentifier.Location);

        AnalyzeExpression(statement.AssignExpr);
        TypeSymbol assignExprType = _typeEvaluator.Evaluate(statement.AssignExpr);

        if (variable != null && assignExprType != Symbol.Void && variable.VariableType != assignExprType)
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"impossible de convertir '{assignExprType.Name}' en '{variable.VariableType.Name}'", statement.NameIdentifier.Location);
    }

    private void AnalyzeFuncDeclStatement(FuncDeclStatement statement)
    {
        string functionName = statement.NameToken.Value;
        if (_symbols.HasSymbol(functionName, SymbolType.Function))
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"impossible de déclarer la fonction '{functionName}' car une fonction avec le même nom existe déjà.", statement.NameToken.Location);

        string typeName = statement.TypeToken.Value;
        if (!_symbols.TryGetType(typeName, out TypeSymbol? returnTypeSymbol))
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"le type '{typeName}' n'est pas reconnu comme étant un type valide.", statement.TypeToken.Location);

        if (returnTypeSymbol != null && returnTypeSymbol == Symbol.Void)
        {
            _errorHandler.Report(ErrorType.SemanticError, $"le type de retour d'une fonction ne peut pas être void, remplacez 'func' par 'proc' si le comportement est voulu.");
        }

        var parameters = new List<ParamSymbol>();
        foreach (var param in statement.Params)
        {
            if (param.IsParams)
            {
                if (param != statement.Params.Last())
                    _errorHandler.ReportDetailed(ErrorType.SemanticError, "le paramètre spécial 'params' doit se trouver en dernière position.", param.ParamsToken!.Location);

                parameters.Add(new ParamSymbol(true));
            }
            else
            {
                if (!_symbols.TryGetType(param.TypeIdentifier.Value, out TypeSymbol? pType)) 
                    _errorHandler.ReportDetailed(ErrorType.SemanticError, $"le type du paramètre '{param.TypeIdentifier.Value}' n'est pas reconnu comme étant un type valide.", param.TypeIdentifier.Location);

                if (_symbols.HasSymbol(param.NameIdentifier.Value, SymbolType.Variable))
                    _errorHandler.ReportDetailed(ErrorType.SemanticError, $"une variable avec le même nom existe déjà dans le contexte actuel.", param.NameIdentifier.Location);

                parameters.Add(new ParamSymbol(param.NameIdentifier.Value, pType!));
                _symbols.AddSymbol(new VariableSymbol(param.NameIdentifier.Value, pType!, true));
            }
        }

        if (statement.IsExtern && statement.Body != null)
        {
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"la fonction externe '{statement.NameToken.Value}' ne peut pas déclarer de corps.", statement.NameToken.Location);
        }
        else if (statement.Body != null)
        {
            AnalyzeScopeStatement(statement.Body);

            //TODO: Assert there is one return expression per scope in the function
            var returnStatements = statement.Body.Statements.OfType<ReturnStatement>();
            if (!returnStatements.Any())
            {
                _errorHandler.ReportDetailed(ErrorType.SemanticError, $"la fonction '{functionName}' ne retourne pas de valeur.", statement.NameToken.Location);
            }
            else
            {
                foreach (var retStmt in returnStatements)
                {
                    AnalyzeExpression(retStmt.ReturnExpr);
                    TypeSymbol type = _typeEvaluator.Evaluate(retStmt.ReturnExpr);
                    if (returnTypeSymbol != null && type != Symbol.Void && type != returnTypeSymbol)
                        _errorHandler.ReportDetailed(ErrorType.SemanticError, $"impossible de convertir '{type.Name}' en '{returnTypeSymbol.Name}'.", retStmt.ReturnKeyword.Location);

                    if (retStmt != returnStatements.First())
                        _errorHandler.ReportDetailed(ErrorType.Warning, $"la déclaration de retour n'est jamais atteinte.", retStmt.ReturnKeyword.Location);
                }
            }
        }

        // foreach (var param in parameters)
        // {
        //     _symbols.RemoveSymbol(param.Name, SymbolType.Variable);
        // }
        
        var symbol = new FunctionSymbol(functionName, returnTypeSymbol!, statement.IsExtern, statement.Body != null, parameters);
        _symbols.AddSymbol(symbol);
    }

    private void AnalyzeScopeStatement(ScopeStatement scope)
    {
        foreach (var child in scope.Statements)
        {
            AnalyzeStatement(child);
        }
    }
}