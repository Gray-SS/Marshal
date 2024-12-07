using Marshal.Compiler.Errors;
using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;

namespace Marshal.Compiler.Semantics;

public class SemanticAnalyzer : IVisitor
{
    private readonly SymbolTable _globalTable;
    private readonly ErrorHandler _errorHandler;
    private readonly TypeEvaluator _typeEvaluator;

    public SemanticAnalyzer(SymbolTable globalTable, ErrorHandler errorHandler)
    {
        _globalTable = globalTable;
        _errorHandler = errorHandler;
        _typeEvaluator = new TypeEvaluator(globalTable, errorHandler);
    }

    public void Visit(CompilationUnit unit)
    {
        foreach (var stmt in unit.Statements)
        {
            stmt.Accept(this);
        }
    }

    public void Visit(AssignmentStatement stmt)
    {
        string varName = stmt.NameIdentifier.Value;
        if (!_globalTable.TryGetVariable(varName, out VariableSymbol? variable))
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"la variable '{varName}' n'existe pas dans le contexte actuel.", stmt.NameIdentifier.Location);

        stmt.AssignExpr.Accept(this);
        TypeSymbol assignExprType = _typeEvaluator.Evaluate(stmt.AssignExpr);

        if (variable != null && assignExprType != Symbol.Void && variable.VariableType != assignExprType)
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"impossible de convertir '{assignExprType.Name}' en '{variable.VariableType.Name}'", stmt.NameIdentifier.Location);
    }

    public void Visit(ScopeStatement stmt)
    {
        _globalTable.EnterScope();

        foreach (var child in stmt.Statements)
        {
            child.Accept(this);
        }

        _globalTable.ExitScope();
    }

    public void Visit(FunCallStatement stmt)
    {
        string functionName = stmt.NameIdentifier.Value;
        if (!_globalTable.TryGetFunction(functionName, out FunctionSymbol? funSymbol))
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"impossible d'appeler la fonction '{functionName}' car elle n'existe pas dans le context actuel.", stmt.NameIdentifier.Location);

        if (funSymbol != null)
        {
            if (stmt.Parameters.Count != funSymbol.Params.Count)
            {
                _errorHandler.ReportDetailed(ErrorType.SemanticError, $"la fontion '{functionName}' attend {funSymbol.Params.Count} paramètre(s) mais '{stmt.Parameters.Count}' ont été fourni.", stmt.NameIdentifier.Location);
            }
            else
            {
                for (int i = 0; i < stmt.Parameters.Count; i++)
                {
                    SyntaxExpression exprParam = stmt.Parameters[i];
                    exprParam.Accept(this);

                    TypeSymbol exprType = _typeEvaluator.Evaluate(exprParam); 
                    ParamSymbol funParam = funSymbol.Params[i];
                    
                    if (exprType != Symbol.Void && funParam.Type != exprType)
                    {
                        _errorHandler.ReportDetailed(ErrorType.SemanticError, $"impossible de convertir '{exprType.Name}' en '{funParam.Type.Name}'.", stmt.NameIdentifier.Location);
                    }
                }
            }
        }
    }

    public void Visit(FuncDeclStatement stmt)
    {
        string functionName = stmt.NameToken.Value;
        if (_globalTable.HasSymbol(functionName, SymbolType.Function))
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"impossible de déclarer la fonction '{functionName}' car une fonction avec le même nom existe déjà.", stmt.NameToken.Location);

        string typeName = stmt.TypeToken.Value;
        if (!_globalTable.TryGetType(typeName, out TypeSymbol? returnTypeSymbol))
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"le type '{typeName}' n'est pas reconnu comme étant un type valide.", stmt.TypeToken.Location);

        if (returnTypeSymbol != null && returnTypeSymbol == Symbol.Void)
        {
            _errorHandler.Report(ErrorType.SemanticError, $"le type de retour d'une fonction ne peut pas être void, remplacez 'func' par 'proc' si le comportement est voulu.");
        }

        _globalTable.EnterScope();

        var parameters = new List<ParamSymbol>();
        foreach (var param in stmt.Params)
        {
            if (param.IsParams)
            {
                if (param != stmt.Params.Last())
                    _errorHandler.ReportDetailed(ErrorType.SemanticError, "le paramètre spécial 'params' doit se trouver en dernière position.", param.ParamsToken!.Location);

                parameters.Add(new ParamSymbol(true));
            }
            else
            {
                if (!_globalTable.TryGetType(param.TypeIdentifier.Value, out TypeSymbol? pType)) 
                    _errorHandler.ReportDetailed(ErrorType.SemanticError, $"le type du paramètre '{param.TypeIdentifier.Value}' n'est pas reconnu comme étant un type valide.", param.TypeIdentifier.Location);

                if (_globalTable.HasSymbol(param.NameIdentifier.Value, SymbolType.Variable))
                    _errorHandler.ReportDetailed(ErrorType.SemanticError, $"une variable avec le même nom existe déjà dans le contexte actuel.", param.NameIdentifier.Location);

                param.ParamType = pType!;
                parameters.Add(new ParamSymbol(param.NameIdentifier.Value, pType!));
                _globalTable.AddSymbol(new VariableSymbol(param.NameIdentifier.Value, pType!, true));
            }
        }

        if (stmt.IsExtern && stmt.Body != null)
        {
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"la fonction externe '{stmt.NameToken.Value}' ne peut pas déclarer de corps.", stmt.NameToken.Location);
        }
        else if (stmt.Body != null)
        {
            stmt.Body.Accept(this);

            //TODO: Assert there is one return expression per scope in the function
            var returnStatements = stmt.Body.Statements.OfType<ReturnStatement>();
            if (!returnStatements.Any())
            {
                _errorHandler.ReportDetailed(ErrorType.SemanticError, $"la fonction '{functionName}' ne retourne pas de valeur.", stmt.NameToken.Location);
            }
            else
            {
                foreach (var retStmt in returnStatements)
                {
                    TypeSymbol type = _typeEvaluator.Evaluate(retStmt.ReturnExpr);
                    if (returnTypeSymbol != null && type != Symbol.Void && type != returnTypeSymbol)
                        _errorHandler.ReportDetailed(ErrorType.SemanticError, $"impossible de convertir '{type.Name}' en '{returnTypeSymbol.Name}'.", retStmt.ReturnKeyword.Location);

                    if (retStmt != returnStatements.First())
                        _errorHandler.ReportDetailed(ErrorType.Warning, $"la déclaration de retour n'est jamais atteinte.", retStmt.ReturnKeyword.Location);
                }
            }
        }

        _globalTable.ExitScope();

        stmt.ReturnType = returnTypeSymbol;
        
        var symbol = new FunctionSymbol(functionName, returnTypeSymbol!, stmt.IsExtern, stmt.Body != null, parameters);
        _globalTable.AddSymbol(symbol);
    }

    public void Visit(ReturnStatement stmt)
    {
        stmt.ReturnExpr.Accept(this);
    }

    public void Visit(VarDeclStatement stmt)
    {
        string varName = stmt.NameIdentifier.Value;
        if (_globalTable.HasSymbol(varName, SymbolType.Variable))
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"une variable avec le nom '{varName}' existe déjà dans ce contexte.", stmt.NameIdentifier.Location);

        string typeName = stmt.TypeIdentifier.Value;
        if (!_globalTable.TryGetType(typeName, out TypeSymbol? varType))
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"le type '{typeName}' n'est pas reconnu comme étant un type valide.", stmt.TypeIdentifier.Location);

        if (varType != null && varType == Symbol.Void)
        {
            _errorHandler.Report(ErrorType.SemanticError, $"le type d'une variable ne peut pas être void.");
        }

        if (varType != null && stmt.InitExpression != null)
        {
            stmt.InitExpression.Accept(this);
            TypeSymbol initExprType = _typeEvaluator.Evaluate(stmt.InitExpression);

            if (initExprType != Symbol.Void && initExprType != varType)
                _errorHandler.ReportDetailed(ErrorType.SemanticError, $"impossible de convertir le type '{initExprType.Name}' en '{varType.Name}'.", stmt.NameIdentifier.Location);
        }

        stmt.VarType = varType;

        var variable = new VariableSymbol(varName, varType!, true);
        _globalTable.AddSymbol(variable);
    }

    public void Visit(FunCallExpression expr)
    {
        string functionName = expr.NameIdentifier.Value;
        if (!_globalTable.TryGetFunction(functionName, out FunctionSymbol? funSymbol))
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
                    exprParam.Accept(this);

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

    public void Visit(LiteralExpression expr)
    {
    }

    public void Visit(VarRefExpression expr)
    {
        string varName = expr.NameIdentifier.Value;
        if (!_globalTable.HasSymbol(varName, SymbolType.Variable))
            _errorHandler.ReportDetailed(ErrorType.SemanticError, $"la variable '{varName}' n'est pas définie.", expr.NameIdentifier.Location);
    }
}