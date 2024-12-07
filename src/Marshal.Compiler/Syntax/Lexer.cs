using Marshal.Compiler.Errors;
using Marshal.Compiler.Syntax;

namespace Marshal.Compiler;

public class Lexer
{
    public CompilationContext Context { get; }
    public int Position { get; private set; }
    public char CurrentChar => Peek(0);
    private Location CurrentLoc => new(_column, _line, Context.Source.RelativePath);

    private int _line;
    private int _column;
    private readonly ErrorHandler _errorHandler;

    public Lexer(CompilationContext ctx, ErrorHandler errorHandler)
    {
        Context = ctx;

        _line = 1;
        _column = 1;
        _errorHandler = errorHandler;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        int cycles = 0;

        while (Position < Context.Source.Content.Length)
        {
            int pos = Position;

            while (Position < Context.Source.Content.Length && char.IsWhiteSpace(CurrentChar))
            {
                if (CurrentChar == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else if (CurrentChar == '\t')
                {
                    int tabSize = 4;
                    _column += tabSize - ((_column - 1) % tabSize);
                }
                else
                {
                    _column++;
                }
                Position++;
            }

            if (char.IsDigit(CurrentChar))
            {
                int length = ReadWhile(char.IsDigit);
                tokens.Add(ReadToken(TokenType.IntLiteral, length));
                continue;
            }

            if (CurrentChar == '"')
            {
                Position++;
                int length = ReadWhile((c) => c != '"');
                tokens.Add(ReadToken(TokenType.StringLiteral, length));
                Position++;
                continue;
            }

            if (char.IsLetter(CurrentChar))
            {
                int length = ReadWhile(char.IsLetterOrDigit);
                ReadOnlySpan<char> span = Context.Source.Content.AsSpan().Slice(Position, length);
                
                TokenType type = span switch
                {
                    "proc" => TokenType.ProcKeyword,
                    "func" => TokenType.FuncKeyword,
                    "var" => TokenType.VarKeyword,
                    "return" => TokenType.ReturnKeyword,
                    "extern" => TokenType.ExternKeyword,
                    "params" => TokenType.ParamsKeyword,
                    _ => TokenType.Identifier
                };

                tokens.Add(ReadToken(type, length));
                continue;
            }

            switch (CurrentChar)
            {
                case '+': tokens.Add(ReadToken(TokenType.Plus, 1)); break;
                case '-': tokens.Add(ReadToken(TokenType.Minus, 1)); break;
                case '*': tokens.Add(ReadToken(TokenType.Asterisk, 1)); break;
                case '/': tokens.Add(ReadToken(TokenType.Slash, 1)); break;
                case '=': tokens.Add(ReadToken(TokenType.Equal, 1)); break;
                case ';': tokens.Add(ReadToken(TokenType.SemiColon, 1)); break;
                case '(': tokens.Add(ReadToken(TokenType.OpenBracket, 1)); break;
                case ')': tokens.Add(ReadToken(TokenType.CloseBracket, 1)); break;
                case '{': tokens.Add(ReadToken(TokenType.OpenCurlyBracket, 1)); break;
                case '}': tokens.Add(ReadToken(TokenType.CloseCurlyBracket, 1)); break;
                case ':': tokens.Add(ReadToken(TokenType.Colon, 1)); break;
                case ',': tokens.Add(ReadToken(TokenType.Comma, 1)); break;
                case '\0': 
                    _errorHandler.ReportDetailed(ErrorType.Warning, $"le caractère de fin de fichier a été trouvé avant la fin du fichier.", CurrentLoc);
                    tokens.Add(ReadToken(TokenType.EOF, 0));
                    return tokens; 
                default:
                    _errorHandler.ReportDetailed(ErrorType.SyntaxError, $"le caractère '{CurrentChar}' n'est pas supporté.", CurrentLoc);
                    tokens.Add(ReadToken(TokenType.Invalid, 1)); 
                    break;
            }

            if (Position == pos)
            {
                cycles++;
                if (cycles >= 5) {
                    _errorHandler.Report(ErrorType.InternalError, "une boucle infinie a été détéctée.");
                    break;
                }
            }
            else cycles = 0;
        }

        tokens.Add(ReadToken(TokenType.EOF, 0));
        return tokens;
    }

    private int ReadWhile(Predicate<char> predicate)
    {
        int length = 0;
        while (predicate.Invoke(Peek(length)))
            length++;
        return length;
    }

    private Token ReadToken(TokenType type, int length)
    {
        int start = Position;
        Position += length;

        string value = Context.Source.Content.Substring(start, length);
        var token = new Token(type, value, CurrentLoc);

        _column += length;
        return token;
    }

    private char Peek(int offset)
    {
        int pos = Position + offset;
        if (pos >= Context.Source.Content.Length)
            return '\0';

        return Context.Source.Content[pos];
    }
}