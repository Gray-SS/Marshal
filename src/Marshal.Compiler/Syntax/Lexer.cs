using Marshal.Compiler.Errors;
using Marshal.Compiler.Syntax;

namespace Marshal.Compiler;

public class Lexer : CompilerPass
{
    public int Position { get; private set; }
    public char CurrentChar => Peek(0);
    private Location CurrentLoc => new(_column, _line, Context.RelativePath);

    private int _line;
    private int _column;

    public Lexer(CompilationContext source, ErrorHandler errorHandler) : base(source, errorHandler)
    {
        _line = 1;
        _column = 1;
    }

    public override void Apply()
    {
        Context.Tokens = Tokenize();
    }

    private List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        int cycles = 0;

        while (Position < Context.Content.Length)
        {
            int pos = Position;

            while (Position < Context.Content.Length && char.IsWhiteSpace(CurrentChar))
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

            if (CurrentChar == '\'' && Peek(1) != '\0' && Peek(2) == '\'')
            {
                Advance(1);
                tokens.Add(ReadToken(TokenType.CharLiteral, 1));
                Advance(1);
                continue;
            }

            if (CurrentChar == '"')
            {
                Advance(1);
                int length = ReadWhile((c) => c != '"');

                if (length == -1)
                {
                    ReportDetailed(ErrorType.SyntaxError, "le string ne se termine jamais.", CurrentLoc);
                    Advance(1);
                    continue;
                }

                tokens.Add(ReadToken(TokenType.StringLiteral, length));
                Advance(1);
                continue;
            }

            if (char.IsLetter(CurrentChar))
            {
                int length = ReadWhile(char.IsLetterOrDigit);
                ReadOnlySpan<char> span = Context.Content.AsSpan().Slice(Position, length);
                
                TokenType type = span switch
                {
                    "proc" => TokenType.ProcKeyword,
                    "func" => TokenType.FuncKeyword,
                    "var" => TokenType.VarKeyword,
                    "new" => TokenType.NewKeyword,
                    "return" => TokenType.ReturnKeyword,
                    "extern" => TokenType.ExternKeyword,
                    "params" => TokenType.ParamsKeyword,
                    "true" => TokenType.TrueKeyword,
                    "false" => TokenType.FalseKeyword,
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
                case '[': tokens.Add(ReadToken(TokenType.OpenSquareBracket, 1)); break;
                case ']': tokens.Add(ReadToken(TokenType.CloseSquareBracket, 1)); break;
                case ':': tokens.Add(ReadToken(TokenType.Colon, 1)); break;
                case ',': tokens.Add(ReadToken(TokenType.Comma, 1)); break;
                case '\0': 
                    tokens.Add(ReadToken(TokenType.EOF, 0));
                    return tokens; 
                default:
                    ReportDetailed(ErrorType.SyntaxError, $"le caractère '{CurrentChar}' n'est pas supporté.", CurrentLoc);
                    tokens.Add(ReadToken(TokenType.Invalid, 1)); 
                    break;
            }

            if (Position == pos)
            {
                cycles++;
                if (cycles >= 5) {
                    Report(ErrorType.InternalError, "une boucle infinie a été détéctée.");
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
        while (predicate.Invoke(Peek(length)) && Peek(length) != '\0')
            length++;

        if (Peek(length) == '\0')
            return -1;

        return length;
    }

    private void Advance(int length)
    {
        for (int i = 0; i < length; i++)
        {
            if (Position < Context.Content.Length)
            {
                if (Context.Content[Position] == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
                Position++;
            }
        }
    }

    private Token ReadToken(TokenType type, int length)
    {
        int start = Position;
        var cloc = CurrentLoc;

        Advance(length);

        string value = Context.Content.Substring(start, length);
        var token = new Token(type, value, cloc);

        return token;
    }

    private char Peek(int offset)
    {
        int pos = Position + offset;
        if (pos >= Context.Content.Length)
            return '\0';

        return Context.Content[pos];
    }
}