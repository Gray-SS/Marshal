using Marshal.Compiler.Errors;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;

namespace Marshal.Compiler.Syntax;

public class Parser
{
    public CompilationContext Context { get; }
    public int Position { get; private set; }
    public Token CurrentToken => Peek(0);

    private readonly List<Token> _tokens;
    private readonly ErrorHandler _errorHandler;

    public Parser(List<Token> tokens, CompilationContext ctx, ErrorHandler errorHandler)
    {
        Context = ctx;
        _tokens = tokens;
        _errorHandler = errorHandler;
    }

    public CompilationUnit ParseProgram()
    {
        var statements = new List<SyntaxStatement>();
        while (CurrentToken.Type != TokenType.EOF)
        {
            try
            {
                var statement = ParseStatement();
                statements.Add(statement);
            }
            catch (ParseException ex)
            {
                if (ex is ParseDetailedException dex)
                    _errorHandler.ReportDetailed(ErrorType.SyntaxError, ex.Message, dex.Location);
                else
                    _errorHandler.Report(ErrorType.SyntaxError, ex.Message);

                Synchronize();
            }
        }

        return new CompilationUnit(statements);
    }

    private SyntaxStatement ParseStatement()
    {
        if (CurrentToken.Type == TokenType.FuncKeyword)
            return ParseFuncDeclStatement();
        else if (CurrentToken.Type == TokenType.ReturnKeyword)
            return ParseReturnStatement();
        else if (CurrentToken.Type == TokenType.VarKeyword)
            return ParseVarDeclStatement();
        else if (CurrentToken.Type == TokenType.Identifier && Peek(1).Type == TokenType.OpenBracket)
            return ParseFunCallStatement();
        else if (CurrentToken.Type == TokenType.Identifier)
            return ParseAssignmentStatement();

        throw new ParseDetailedException($"token inattendu '{CurrentToken.Value}'.", CurrentToken.Location);
    }

    private AssignmentStatement ParseAssignmentStatement()
    {
        Token nameIdentifier = Expect(TokenType.Identifier, "identifiant de la variable attendu pour l'assignement d'une variable");
        Expect(TokenType.Equal, "signe égale '=' attendu après l'identifiant de la variable.");

        SyntaxExpression assignExpr = ParseExpression();
        Expect(TokenType.SemiColon, "point-virgule ';' attendu après l'assignement d'une variable.");

        return new AssignmentStatement(nameIdentifier, assignExpr);
    }

    private FuncDeclStatement ParseFuncDeclStatement()
    {
        Expect(TokenType.FuncKeyword, "mot clé 'func' attendu pour la déclaration de fonction.");

        bool isExtern = false;
        if (CurrentToken.Type == TokenType.ExternKeyword)
        {
            NextToken();
            isExtern = true;
        }

        Token nameIdentifier = Expect(TokenType.Identifier, "identifiant de fonction attendu après 'func'.");
        
        Expect(TokenType.OpenBracket, "parenthèse ouvrante '(' attendue après le nom de la fonction.");
        
        var parameters = new List<SyntaxFuncDeclParam>();
        while (CurrentToken.Type != TokenType.EOF && 
               CurrentToken.Type != TokenType.CloseBracket)
        {
            if (CurrentToken.Type == TokenType.ParamsKeyword)
            {
                Token paramsKeyword = NextToken();
                parameters.Add(new SyntaxFuncDeclParam(paramsKeyword));
            }
            else
            {
                Token pTypeId = Expect(TokenType.Identifier, "le type du paramètre est attendu.");
                Token pNameId = Expect(TokenType.Identifier, "le nom de la variable est attendue.");
                parameters.Add(new SyntaxFuncDeclParam(pTypeId, pNameId));
            }

            if (CurrentToken.Type == TokenType.Comma) NextToken();
            else break;
        }
        
        Expect(TokenType.CloseBracket, "parenthèse fermante ')' attendue après la liste des paramètres.");

        Expect(TokenType.Colon, "le type de retour de la fonction est attendu après les paramètre de la fonction.");
        Token typeIdentifier = Expect(TokenType.Identifier, "type de retour attendu après les deux-points.");

        ScopeStatement? body = null;
        if (CurrentToken.Type != TokenType.SemiColon)
        {
            body = ParseScopeStatement() ?? throw new ParseException("le corps de la fonction est vide ou incorrect.");
        }
        else NextToken();

        return new FuncDeclStatement(nameIdentifier, typeIdentifier, parameters, body, isExtern);
    }

    private VarDeclStatement ParseVarDeclStatement()
    {
        Expect(TokenType.VarKeyword, "mot clé 'var' attendu pour la déclaration de variable.");
        Token nameIdentifier = Expect(TokenType.Identifier, "identifiant de la variable attendu après 'var'.");

        Expect(TokenType.Colon, "deux-points ':' attendu après l'identifiant de la variable.");
        Token typeIdentifier = Expect(TokenType.Identifier, "le type de la variable est attendu après les deux points ':'.");

        SyntaxExpression? initExpr = null;
        if (CurrentToken.Type == TokenType.Equal)
        {
            NextToken();
            initExpr = ParseExpression();
        }

        Expect(TokenType.SemiColon, "point-virgule ';' attendu après la déclaration d'une variable.");

        return new VarDeclStatement(nameIdentifier, typeIdentifier, initExpr);
    }

    private FunCallStatement ParseFunCallStatement()
    {
        Token token = Expect(TokenType.Identifier, "un identifiant est attendu pour l'appel d'une fonction.");
        Expect(TokenType.OpenBracket, "une parenthèse ouvrante '(' est attendue après le nom de la fonction a appelé.");

        var parameters = new List<SyntaxExpression>();
        while (CurrentToken.Type != TokenType.EOF &&
               CurrentToken.Type != TokenType.CloseBracket)
        {
            SyntaxExpression expr = ParseExpression();
            parameters.Add(expr);

            if (CurrentToken.Type == TokenType.Comma) NextToken();
            else break;
        }

        Expect(TokenType.CloseBracket, "une parenthèse fermante ')' est attentue après la définition des paramètres de la fonction.");
        Expect(TokenType.SemiColon, "point-virgule ';' attendu après l'appel à une fonction.");

        return new FunCallStatement(token, parameters);
    }

    private ReturnStatement ParseReturnStatement()
    {
        Token returnKeyword = Expect(TokenType.ReturnKeyword, "mot clé 'return' attendu pour la déclaration de retour.");

        SyntaxExpression returnExpr = ParseExpression();
        Expect(TokenType.SemiColon, "point-virgule ';' attendu après la déclaration de retour.");

        return new ReturnStatement(returnKeyword, returnExpr);
    }

    private ScopeStatement ParseScopeStatement()
    {
        Expect(TokenType.OpenCurlyBracket, "accolade ouvrante '{' attendue pour le début du bloc.");

        var statements = new List<SyntaxStatement>();
        
        while (CurrentToken.Type != TokenType.CloseCurlyBracket)
        {
            var statement = ParseStatement();
            statements.Add(statement);
        }

        Expect(TokenType.CloseCurlyBracket, "accolade fermante '}' attendue pour la fin du bloc.");
        return new ScopeStatement(statements);
    }

    private SyntaxExpression ParseExpression()
    {
        if (CurrentToken.Type == TokenType.Identifier)
        {
            if (Peek(1).Type == TokenType.OpenBracket)
                return ParseFunCallExpression();

            return ParseVarRefExpression();
        }

        return ParseLiteralExpression();
    }

    private LiteralExpression ParseLiteralExpression()
    {
        Token token = Expect(TokenType.NumberLiteral, "un literal est attendu pour une expression de type litérale.");
        return new LiteralExpression(token);
    }

    private VarRefExpression ParseVarRefExpression()
    {
        Token token = Expect(TokenType.Identifier, "un identifiant est attendu pour la référence d'une variable.");
        return new VarRefExpression(token);
    }

    private FunCallExpression ParseFunCallExpression()
    {
        Token token = Expect(TokenType.Identifier, "un identifiant est attendu pour l'appel d'une fonction.");
        Expect(TokenType.OpenBracket, "une parenthèse ouvrante '(' est attendue après le nom de la fonction a appelé.");

        var parameters = new List<SyntaxExpression>();
        while (CurrentToken.Type != TokenType.EOF &&
               CurrentToken.Type != TokenType.CloseBracket)
        {
            SyntaxExpression expr = ParseExpression();
            parameters.Add(expr);

            if (CurrentToken.Type == TokenType.Comma) NextToken();
            else break;
        }

        Expect(TokenType.CloseBracket, "une parenthèse fermante ')' est attentue après la définition des paramètres de la fonction.");

        return new FunCallExpression(token, parameters);
    }

    private Token Expect(TokenType expectedType, string errorMessage)
    {
        Token token = NextToken();

        if (token.Type != expectedType)
        {
            throw new ParseDetailedException(errorMessage, token.Location);
        }

        return token;
    }

    private void Synchronize()
    {
        while (CurrentToken.Type != TokenType.EOF &&
               CurrentToken.Type != TokenType.SemiColon &&
               CurrentToken.Type != TokenType.VarKeyword &&
               CurrentToken.Type != TokenType.Identifier &&
               CurrentToken.Type != TokenType.FuncKeyword &&
               CurrentToken.Type != TokenType.OpenCurlyBracket)
        {
            NextToken();
        }

        if (CurrentToken.Type == TokenType.SemiColon)
            NextToken();
    }

    private Token NextToken()
    {
        Token token = _tokens[Position++];
        return token;
    }

    private Token Peek(int offset)
    {
        int pos = Math.Min(Position + offset, _tokens.Count - 1);
        return _tokens[pos];
    }
}

public class ParseException : Exception
{
    public ParseException(string message) : base(message) 
    {
    }
}

public class ParseDetailedException : ParseException
{
    public Location Location { get; }

    public ParseDetailedException(string message, Location location) : base(message)
    {
        Location = location;
    }
}