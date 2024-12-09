using Marshal.Compiler.Errors;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;
using Marshal.Compiler.Utilities;

namespace Marshal.Compiler.Syntax;

public class Parser : CompilerPass
{
    public int Position { get; private set; }
    public Token CurrentToken => Peek(0);

    public Parser(CompilationContext context, ErrorHandler errorHandler) : base(context, errorHandler)
    {
    }

    public override void Apply()
    {
        Context.AST = ParseAST();
    }

    public CompilationUnit ParseAST()
    {
        var statements = new List<SyntaxStatement>();
        while (CurrentToken.Type != TokenType.EOF)
        {
            try
            {
                var statement = ParseStatement();
                statements.Add(statement);
            }
            catch (CompilerException ex)
            {
                if (ex is CompilerDetailedException dex)
                    ReportDetailed(ErrorType.SyntaxError, ex.Message, dex.Location);
                else
                    Report(ErrorType.SyntaxError, ex.Message);

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
        else if (CurrentToken.Type == TokenType.OpenCurlyBracket)
            return ParseScopeStatement();

        throw new CompilerDetailedException(ErrorType.SyntaxError, $"token inattendu '{CurrentToken.Value}'.", CurrentToken.Loc);
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
        
        var parameters = new List<FuncParamDeclNode>();
        while (CurrentToken.Type != TokenType.EOF && 
               CurrentToken.Type != TokenType.CloseBracket)
        {
            SyntaxTypeNode pTypeId = ParseType("le type du paramètre est attendu.");
            Token pNameId = Expect(TokenType.Identifier, "le nom de la variable est attendue.");
            parameters.Add(new FuncParamDeclNode(pTypeId, pNameId));

            if (CurrentToken.Type == TokenType.Comma) NextToken();
            else break;
        }
        
        Expect(TokenType.CloseBracket, "parenthèse fermante ')' attendue après la liste des paramètres.");

        Expect(TokenType.Colon, "le type de retour de la fonction est attendu après les paramètre de la fonction.");
        SyntaxTypeNode returnType = ParseType("type de retour attendu après les deux-points.");

        ScopeStatement? body = null;
        if (CurrentToken.Type != TokenType.SemiColon)
        {
            body = ParseScopeStatement() ?? throw new CompilerException(ErrorType.SyntaxError, "le corps de la fonction est vide ou incorrect.");
        }
        else NextToken();

        return new FuncDeclStatement(nameIdentifier, returnType, parameters, body, isExtern);
    }

    private VarDeclStatement ParseVarDeclStatement()
    {
        Expect(TokenType.VarKeyword, "mot clé 'var' attendu pour la déclaration de variable.");
        Token nameIdentifier = Expect(TokenType.Identifier, "identifiant de la variable attendu après 'var'.");

        Expect(TokenType.Colon, "deux-points ':' attendu après l'identifiant de la variable.");
        SyntaxTypeNode type = ParseType("le type de la variable est attendu après les deux points ':'.");

        SyntaxExpression? initExpr = null;
        if (CurrentToken.Type == TokenType.Equal)
        {
            NextToken();
            initExpr = ParseExpression();
        }

        Expect(TokenType.SemiColon, "point-virgule ';' attendu après la déclaration d'une variable.");

        return new VarDeclStatement(nameIdentifier, type, initExpr);
    }

    private SyntaxTypeNode ParseType(string errorMessage)
    {
        SyntaxTypeNode type = ParsePrimitiveType(errorMessage);
        
        while (CurrentToken.Type == TokenType.Asterisk ||
               CurrentToken.Type == TokenType.OpenSquareBracket)
        {
            if (CurrentToken.Type == TokenType.Asterisk)
            {
                NextToken();
                type = new SyntaxPointerType(type);
            }
            else if (CurrentToken.Type == TokenType.OpenSquareBracket)
            {
                NextToken();

                int length = 0;
                if (CurrentToken.Type == TokenType.IntLiteral)
                {
                    var lengthToken = NextToken();
                    length = int.Parse(lengthToken.Value);
                }

                Expect(TokenType.CloseSquareBracket, "un crochet de fermeture ']' est attendu après l'ouverture du crochet.");
                type = new SyntaxArrayType(type, length);
            }
        }

        return type;
    }

    private SyntaxTypeNode ParsePrimitiveType(string errorMessage)
    {
        Token identifier = Expect(TokenType.Identifier, errorMessage);
        return new SyntaxPrimitiveType(identifier);
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
        if (CurrentToken.Type == TokenType.OpenCurlyBracket) 
            return ParseArrayExpression();

        return ParseBinOpExpression();
    }

    private ArrayInitExpression ParseArrayExpression()
    {
        Expect(TokenType.OpenCurlyBracket, "accolade ouvrante '{' est attendue pour une expression initialiseur de tableau.");

        var expressions = new List<SyntaxExpression>();

        while (CurrentToken.Type != TokenType.EOF && 
               CurrentToken.Type != TokenType.CloseCurlyBracket)
        {
            var expr = ParseBinOpExpression();
            expressions.Add(expr);

            if (CurrentToken.Type == TokenType.Comma)
                NextToken();
            else
                break;
        } 

        Expect(TokenType.CloseCurlyBracket, "accolade fermante '}' attendue pour la fin de l'expression initialiseur de tableau.");

        return new ArrayInitExpression(expressions);
    }

    private SyntaxExpression ParseBinOpExpression(int precedence = 0)
    {
        var left = ParsePrimaryExpression();

        while (true)
        {
            var opType = GetBinaryOperatorType(CurrentToken.Type);
            if (opType == null)
                break;

            int opPrecedence = GetOperatorPrecedence(CurrentToken.Type);
            if (opPrecedence < precedence)
                break;

            NextToken();

            var right = ParseBinOpExpression(opPrecedence + 1);

            left = new BinaryOpExpression(left, right, opType.Value);
        }

        return left;
    }

    private static BinOperatorType? GetBinaryOperatorType(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Plus => BinOperatorType.Addition,
            TokenType.Minus => BinOperatorType.Subtraction,
            TokenType.Asterisk => BinOperatorType.Multiplication,
            TokenType.Slash => BinOperatorType.Division,
            _ => null
        };
    }

    private static int GetOperatorPrecedence(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Plus => 1, // Lowest precedence
            TokenType.Minus => 1, // Lowest precedence
            TokenType.Asterisk => 2, // Higher precedence
            TokenType.Slash => 2, // Higher precedence
            _ => 0
        };
    }


    private SyntaxExpression ParsePrimaryExpression()
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
        Token token = NextToken();
        LiteralType literalType = token.GetLiteralType();

        if (literalType == LiteralType.None)
            throw new CompilerDetailedException(ErrorType.SyntaxError, "le token actuel n'est pas reconnu comme étant un litéral valide.", token.Loc);

        return new LiteralExpression(token, literalType);
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
            throw new CompilerDetailedException(ErrorType.SyntaxError, errorMessage, token.Loc);
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
        Token token = Context.Tokens![Position++];
        return token;
    }

    private Token Peek(int offset)
    {
        int pos = Math.Min(Position + offset, Context.Tokens!.Count - 1);
        return Context.Tokens![pos];
    }
}

public class CompilerException : Exception
{
    public ErrorType ErrorType { get; }
    
    public CompilerException(ErrorType errorType, string message) : base(message) 
    {
        ErrorType = errorType;
    }
}

public class CompilerDetailedException : CompilerException
{
    public Location Location { get; }

    public CompilerDetailedException(ErrorType errorType, string message, Location location) : base(errorType, message)
    {
        Location = location;
    }
}