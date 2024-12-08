namespace Marshal.Compiler.Syntax;

public class Token
{
    public Location Loc { get; }
    public string Value { get; }
    public TokenType Type { get; }

    public Token(TokenType type, string value, Location location)
    {
        Type = type;
        Value = value;
        Loc = location;
    }
}