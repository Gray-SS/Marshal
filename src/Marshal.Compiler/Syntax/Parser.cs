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
        Context.AST.Dump();
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
        else if (CurrentToken.Type == TokenType.Star && Peek(1).Type == TokenType.Identifier)
            return ParseAssignmentStatement();
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
        Location loc = CurrentToken.Loc;
        Expect(TokenType.WhileKeyword, "le mot clé 'while' est attendu afin de créer une boucle.");
        Expect(TokenType.OpenBracket, "une parenthèse ouvrante '(' est attendue après le mot-clé 'while'.");
        var condExpr = ParseExpression();
        Expect(TokenType.CloseBracket, "une parenthèse fermante ')' est attendue après l'expression conditionnelle.");
        var scope = ParseScopeStatement();

        return new WhileStatement(loc, condExpr, scope);
    }

    private IncrementStatement ParseIncrementStatement()
    {
        IncrementStatement result;

        Location loc = CurrentToken.Loc;
        Token varNameToken = Expect(TokenType.Identifier, "l'identifiant de la variable a incrémenté est attendue.");
        if (CurrentToken.Type == TokenType.Plus)
        {
            NextToken();
            Expect(TokenType.Plus, "symbole plus '+' attendu après le '+'.");
            result = new IncrementStatement(loc, varNameToken, false);
        }
        else if (CurrentToken.Type == TokenType.Minus)
        {
            NextToken();
            Expect(TokenType.Minus, "symbole moins '-' attendu après le '-'.");
            result = new IncrementStatement(loc, varNameToken, true);
        }
        else throw new InvalidOperationException();

        Expect(TokenType.SemiColon, "point virgule ';' attendu après l'incrémentation de la variable.");
        return result;
    }

    private IfStatement ParseIfStatement()
    {
        Location loc = CurrentToken.Loc;
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

        return new IfStatement(loc, ifsScopes, elseScope);
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
        Location loc = CurrentToken.Loc;
        SyntaxExpression locatorExpr = ParseExpression();
        if (locatorExpr.ValueCategory != ValueCategory.Locator)
            throw new CompilerDetailedException(ErrorType.SyntaxError, $"une expression de type locator est attendue pour la partie gauche de l'assignement.", locatorExpr.Loc);

        Token identifierToken = GetIdentifierToken(locatorExpr);
        Expect(TokenType.Equal, "signe égale '=' attendu après l'identifiant de la variable.");

        SyntaxExpression assignExpr = ParseExpression();
        Expect(TokenType.SemiColon, "point-virgule ';' attendu après l'assignement d'une variable.");

        return new AssignmentStatement(loc, identifierToken, locatorExpr, assignExpr);
    }

    private static Token GetIdentifierToken(SyntaxExpression expr)
    {
        if (expr is VarRefExpression varRef)
            return varRef.NameToken;
        else if (expr is ArrayAccessExpression arrAccessExpr)
            return GetIdentifierToken(arrAccessExpr.ArrayExpr);
        else if (expr is UnaryOpExpression unaryOpExpr)
            return GetIdentifierToken(unaryOpExpr.Operand);

        throw new InvalidOperationException("Couldn't find the identifier token.");
    }

    private FuncDeclStatement ParseFuncDeclStatement()
    {
        Location loc = CurrentToken.Loc;
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

        return new FuncDeclStatement(loc, nameIdentifier, returnType, parameters, body, isExtern);
    }

    private VarDeclStatement ParseVarDeclStatement()
    {
        Location loc = CurrentToken.Loc;
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

        return new VarDeclStatement(loc, nameIdentifier, type, initExpr);
    }

    private SyntaxTypeNode ParseType(string errorMessage)
    {
        SyntaxTypeNode type = ParsePrimitiveType(errorMessage);
        
        while (CurrentToken.Type == TokenType.Star ||
               CurrentToken.Type == TokenType.OpenSquareBracket)
        {
            if (CurrentToken.Type == TokenType.Star)
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
        Location loc = CurrentToken.Loc;
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

        return new FunCallStatement(loc, token, parameters);
    }

    private ReturnStatement ParseReturnStatement()
    {
        Location loc = CurrentToken.Loc;
        Token returnKeyword = Expect(TokenType.ReturnKeyword, "mot clé 'return' attendu pour la déclaration de retour.");

        SyntaxExpression returnExpr = ParseExpression();
        Expect(TokenType.SemiColon, "point-virgule ';' attendu après la déclaration de retour.");

        return new ReturnStatement(loc, returnKeyword, returnExpr);
    }

    private ScopeStatement ParseScopeStatement()
    {
        Location loc = CurrentToken.Loc;
        Expect(TokenType.OpenCurlyBracket, "accolade ouvrante '{' attendue pour le début du bloc.");

        var statements = new List<SyntaxStatement>();
        
        while (CurrentToken.Type != TokenType.CloseCurlyBracket)
        {
            var statement = ParseStatement();
            statements.Add(statement);
        }

        Expect(TokenType.CloseCurlyBracket, "accolade fermante '}' attendue pour la fin du bloc.");
        return new ScopeStatement(loc, statements);
    }

    private SyntaxExpression ParseExpression()
    {
        Location loc = CurrentToken.Loc;
        SyntaxExpression expr = CurrentToken.Type switch {
            TokenType.OpenCurlyBracket => ParseArrayExpression(),
            TokenType.NewKeyword => ParseNewExpression(),
            _ => ParseBinOpExpression(),
        };

        return expr;
    }

    private BracketExpression ParseBracketExpression()
    {
        Location loc = CurrentToken.Loc;
        Expect(TokenType.OpenBracket, "une parenthèse ouvrante '(' est attendue pour une expression entre parenthèse.");
        SyntaxExpression expr = ParseExpression();
        Expect(TokenType.CloseBracket, "une parenthèse fermante ')' est attendue pour une expression entre parenthèse.");

        return new BracketExpression(loc, expr);
    }

    private NewExpression ParseNewExpression()
    {
        Location loc = CurrentToken.Loc;
        Expect(TokenType.NewKeyword, "le mot clé 'new' est attendu pour une expression de type new.");
        Token typeName = Expect(TokenType.Identifier, "le nom du type a allouer est attendu après le mot clé new.");

        if (CurrentToken.Type == TokenType.OpenSquareBracket)
        {
            //Parse the new array expression
            NextToken();
            var lengthExpr = ParseExpression();
            Expect(TokenType.CloseSquareBracket, "une parenthèse carrée fermante ']' est attendue après l'expression de taille du tableau.");

            return new NewArrayExpression(loc, typeName, lengthExpr);
        }

        if (CurrentToken.Type == TokenType.OpenBracket)
        {
            ReportDetailed(ErrorType.Error, "l'allouement d'une classe ou d'une structure n'est pas supporté.", CurrentToken.Loc);
            throw new Exception();
        }

        ReportDetailed(ErrorType.Error, $"le token '{CurrentToken.Value}' n'était pas attendu.", CurrentToken.Loc);
        throw new Exception();
    }

    private ArrayInitExpression ParseArrayExpression()
    {
        Location loc = CurrentToken.Loc;
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

        return new ArrayInitExpression(loc, expressions);
    }

    private SyntaxExpression ParseBinOpExpression(int precedence = 0)
    {
        Location loc = CurrentToken.Loc;
        var left = ParseUnaryExpression();

        while (true)
        {
            BinOpType? opType = GetBinaryOperatorType(CurrentToken.Type);
            if (opType == null) break;

            int opPrecedence = GetOperatorPrecedence(opType.Value);
            if (opPrecedence < precedence) break;

            NextToken();

            var right = ParseBinOpExpression(opPrecedence + 1);

            left = new BinaryOpExpression(loc, left, right, opType.Value);
        }

        return left;
    }

    private SyntaxExpression ParseUnaryExpression()
    {
        Location loc = CurrentToken.Loc;
        UnaryOpType? unaryOpType = GetUnaryOperatorType(CurrentToken.Type);
        if (unaryOpType != null)
        {
            NextToken();

            SyntaxExpression operand = ParseUnaryExpression();
            return new UnaryOpExpression(loc, operand, unaryOpType.Value);
        }

        return ParsePrimaryExpression();
    }

    private static UnaryOpType? GetUnaryOperatorType(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Minus => UnaryOpType.Negation,
            TokenType.Exclamation => UnaryOpType.Not,
            TokenType.Ampersand => UnaryOpType.AddressOf,
            TokenType.Star => UnaryOpType.Deference,
            _ => null
        };
    }

    private static BinOpType? GetBinaryOperatorType(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Plus => BinOpType.Addition,
            TokenType.Minus => BinOpType.Subtraction,
            TokenType.Star => BinOpType.Multiplication,
            TokenType.Slash => BinOpType.Division,
            TokenType.Percent => BinOpType.Modulo,
            TokenType.EqualCond => BinOpType.Equals,
            TokenType.NotEqualCond => BinOpType.NotEquals,
            TokenType.BiggerThanCond => BinOpType.BiggerThan,
            TokenType.BiggerThanEqCond => BinOpType.BiggerThanEq,
            TokenType.LessThanCond => BinOpType.LessThan,
            TokenType.LessThanEqCond => BinOpType.LessThanEq,
            _ => null
        };
    }

    private static int GetOperatorPrecedence(BinOpType opType)
    {
        return opType switch
        {
            BinOpType.Addition => 1,
            BinOpType.Subtraction => 1,
            BinOpType.Modulo => 2,
            BinOpType.LessThan => 2,
            BinOpType.LessThanEq => 2,
            BinOpType.BiggerThan => 2,
            BinOpType.BiggerThanEq => 2,
            BinOpType.Equals => 2,
            BinOpType.NotEquals => 2,
            BinOpType.Multiplication => 3,
            BinOpType.Division => 3,
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
        else if (CurrentToken.Type == TokenType.OpenBracket && Peek(1).Type == TokenType.Identifier)
        {
            return ParseCastExpression();
        }
        else if (CurrentToken.Type == TokenType.OpenBracket)
        {
            return ParseBracketExpression();
        }

        return ParseLiteralExpression();
    }

    private ArrayAccessExpression ParseArrayAccessExpression(SyntaxExpression expr)
    {
        Location loc = CurrentToken.Loc;

        NextToken();
        SyntaxExpression indexExpr = ParseExpression();
        Expect(TokenType.CloseSquareBracket, "une parenthèse carrée fermante ']' est attendue après l'index de l'accès au tableau.");

        return new ArrayAccessExpression(loc, expr, indexExpr);
    }

    private CastExpression ParseCastExpression()
    {
        Location loc = CurrentToken.Loc;
        Expect(TokenType.OpenBracket, "une parenthèse ouvrante '(' est attendue pour une expression de type cast.");
        var castedType = ParseType("le type a casté est attendu entre les parenthèses.");
        Expect(TokenType.CloseBracket, "une parenthèse fermante ')' est attendue après le type du cast.");

        var expr = ParseExpression();

        return new CastExpression(loc, castedType, expr);
    }

    private LiteralExpression ParseLiteralExpression()
    {
        Location loc = CurrentToken.Loc;
        Token token = NextToken();
        LiteralType literalType = token.GetLiteralType();

        if (literalType == LiteralType.None)
            throw new CompilerDetailedException(ErrorType.SyntaxError, "le token actuel n'est pas reconnu comme étant un litéral valide.", token.Loc);

        return new LiteralExpression(loc, token, literalType);
    }

    private SyntaxExpression ParseVarRefExpression()
    {
        Location loc = CurrentToken.Loc;
        Token token = Expect(TokenType.Identifier, "un identifiant est attendu pour la référence d'une variable.");

        SyntaxExpression expr = new VarRefExpression(loc, token);
        while (CurrentToken.Type == TokenType.OpenSquareBracket)
        {
            expr = ParseArrayAccessExpression(expr);
        }

        return expr;
    }

    private FunCallExpression ParseFunCallExpression()
    {
        Location loc = CurrentToken.Loc;
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

        return new FunCallExpression(loc, token, parameters);
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