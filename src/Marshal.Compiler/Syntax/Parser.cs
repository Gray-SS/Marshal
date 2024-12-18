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
        else if (CurrentToken.Type == TokenType.IfKeyword)
            return ParseIfStatement();
        else if (CurrentToken.Type == TokenType.WhileKeyword)
            return ParseWhileStatement();
        else if (CurrentToken.Type == TokenType.ReturnKeyword)
            return ParseReturnStatement();
        else if (CurrentToken.Type == TokenType.VarKeyword)
            return ParseVarDeclStatement();
        else if (CurrentToken.Type == TokenType.Identifier)
        {
            if (Peek(1).Type == TokenType.OpenBracket)
                return ParseFunCallStatement();
            else if (Peek(1).Type == TokenType.Plus && Peek(2).Type == TokenType.Plus)
                return ParseIncrementStatement();
            else if (Peek(1).Type == TokenType.Minus && Peek(2).Type == TokenType.Minus)
                return ParseIncrementStatement();
            
            return ParseAssignmentStatement();
        }
        else if (CurrentToken.Type == TokenType.OpenCurlyBracket)
            return ParseScopeStatement();

        throw new CompilerDetailedException(ErrorType.SyntaxError, $"token inattendu '{CurrentToken.Value}'.", CurrentToken.Loc);
    }

    private WhileStatement ParseWhileStatement()
    {
        Expect(TokenType.WhileKeyword, "le mot clé 'while' est attendu afin de créer une boucle.");
        Expect(TokenType.OpenBracket, "une parenthèse ouvrante '(' est attendue après le mot-clé 'while'.");
        var condExpr = ParseExpression();
        Expect(TokenType.CloseBracket, "une parenthèse fermante ')' est attendue après l'expression conditionnelle.");
        var scope = ParseScopeStatement();

        return new WhileStatement(condExpr, scope);
    }

    private IncrementStatement ParseIncrementStatement()
    {
        IncrementStatement result;
        Token varNameToken = Expect(TokenType.Identifier, "l'identifiant de la variable a incrémenté est attendue.");
        if (CurrentToken.Type == TokenType.Plus)
        {
            NextToken();
            Expect(TokenType.Plus, "symbole plus '+' attendu après le '+'.");
            result = new IncrementStatement(varNameToken, false);
        }
        else if (CurrentToken.Type == TokenType.Minus)
        {
            NextToken();
            Expect(TokenType.Minus, "symbole moins '-' attendu après le '-'.");
            result = new IncrementStatement(varNameToken, true);
        }
        else throw new InvalidOperationException();

        Expect(TokenType.SemiColon, "point virgule ';' attendu après l'incrémentation de la variable.");
        return result;
    }

    private IfStatement ParseIfStatement()
    {
        var ifsScopes = new List<ConditionalScope>
        {
            ParseConditionalScope()
        };

        while (CurrentToken.Type == TokenType.ElseKeyword && Peek(1).Type == TokenType.IfKeyword) {
            //Consume the else token
            NextToken();
            
            var condScope = ParseConditionalScope();
            ifsScopes.Add(condScope);
        }

        ScopeStatement? elseScope = null; 
        if (CurrentToken.Type == TokenType.ElseKeyword)
        {
            NextToken();
            elseScope = ParseScopeStatement();
        }

        return new IfStatement(ifsScopes, elseScope);
    }


    private ConditionalScope ParseConditionalScope()
    {
        Expect(TokenType.IfKeyword, "le mot clé 'if' est attendu afin de créer une déclaration conditionnelle.");
        Expect(TokenType.OpenBracket, "une parenthèse ouvrante '(' est attendue après le mot-clé 'if'.");
        var condExpr = ParseExpression();
        Expect(TokenType.CloseBracket, "une parenthèse fermante ')' est attendue après l'expression conditionnelle.");
        var scope = ParseScopeStatement();

        return new ConditionalScope(scope, condExpr);
    }

    private AssignmentStatement ParseAssignmentStatement()
    {
        Token identifierToken = Expect(TokenType.Identifier, "identifiant ou variable attendue.");
        SyntaxExpression lValue = ParseLValue(identifierToken);
        Expect(TokenType.Equal, "signe égale '=' attendu après l'identifiant de la variable.");

        SyntaxExpression assignExpr = ParseExpression();
        Expect(TokenType.SemiColon, "point-virgule ';' attendu après l'assignement d'une variable.");

        return new AssignmentStatement(identifierToken, lValue, assignExpr);
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
            Token pNameId = Expect(TokenType.Identifier, "le nom du paramètre est attendu.");
            Expect(TokenType.Colon, "une colonne ':' est attendu après le nom du paramètre.");
            SyntaxTypeNode pTypeId = ParseType("le type du paramètre est attendu.");
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

                Expect(TokenType.CloseSquareBracket, "un crochet de fermeture ']' est attendu après l'ouverture du crochet.");
                type = new SyntaxArrayType(type);
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
        SyntaxExpression expr = CurrentToken.Type switch {
            TokenType.OpenCurlyBracket => ParseArrayExpression(),
            TokenType.NewKeyword => ParseNewExpression(),

            _ => ParseBinOpExpression(),
        };

        if (Peek(0).Type == TokenType.OpenSquareBracket)
        {
            //Parse an array access expression
            NextToken();
            SyntaxExpression indexExpr = ParseExpression();
            Expect(TokenType.CloseSquareBracket, "une parenthèse carrée fermante ']' est attendue après l'index de l'accès au tableau.");

            expr = new ArrayAccessExpression(expr, indexExpr);
        }

        return expr;
    }

    private NewExpression ParseNewExpression()
    {
        Expect(TokenType.NewKeyword, "le mot clé 'new' est attendu pour une expression de type new.");
        Token typeName = Expect(TokenType.Identifier, "le nom du type a allouer est attendu après le mot clé new.");

        if (CurrentToken.Type == TokenType.OpenSquareBracket)
        {
            //Parse the new array expression
            NextToken();
            var lengthExpr = ParseExpression();
            Expect(TokenType.CloseSquareBracket, "une parenthèse carrée fermante ']' est attendue après l'expression de taille du tableau.");

            return new NewArrayExpression(typeName, lengthExpr);
        }

        if (CurrentToken.Type == TokenType.OpenBracket)
        {
            ReportDetailed(ErrorType.Error, "l'allouement d'une classe ou d'une structure n'est pas supporté.", CurrentToken.Loc);
            throw new Exception();
        }

        ReportDetailed(ErrorType.Error, $"le token '{CurrentToken.Value}' n'était pas attendu.", CurrentToken.Loc);
        throw new Exception();
    }

    private SyntaxExpression ParseLValue(Token identifierToken)
    {
        var lValueExpr = new VarRefExpression(identifierToken);

        if (CurrentToken.Type == TokenType.OpenSquareBracket)
        {
            NextToken();

            SyntaxExpression indexExpr = ParseExpression();
            
            Expect(TokenType.CloseSquareBracket, "crochet fermant attendu.");

            return new ArrayAccessExpression(lValueExpr, indexExpr);
        }
        
        return lValueExpr;
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
            BinOperatorType? opType = GetBinaryOperatorType(CurrentToken.Type);
            if (opType == null) break;

            int opPrecedence = GetOperatorPrecedence(opType.Value);
            if (opPrecedence < precedence) break;

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
            TokenType.EqualCond => BinOperatorType.Equals,
            TokenType.NotEqualCond => BinOperatorType.NotEquals,
            TokenType.BiggerThanCond => BinOperatorType.BiggerThan,
            TokenType.BiggerThanEqCond => BinOperatorType.BiggerThanEq,
            TokenType.LessThanCond => BinOperatorType.LessThan,
            TokenType.LessThanEqCond => BinOperatorType.LessThanEq,
            _ => null
        };
    }

    private static int GetOperatorPrecedence(BinOperatorType opType)
    {
        return opType switch
        {
            BinOperatorType.Addition => 1,
            BinOperatorType.Subtraction => 1,
            BinOperatorType.Multiplication => 2,
            BinOperatorType.Division => 2,
            BinOperatorType.LessThan => 3,
            BinOperatorType.LessThanEq => 3,
            BinOperatorType.BiggerThan => 3,
            BinOperatorType.BiggerThanEq => 3,
            BinOperatorType.Equals => 3,
            BinOperatorType.NotEquals => 3,
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