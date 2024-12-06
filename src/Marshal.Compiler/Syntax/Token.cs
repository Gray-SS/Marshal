namespace Marshal.Compiler.Syntax;

public class Token
{
    public Location Location { get; }
    public string Value { get; }
    public TokenType Type { get; }

    public Token(TokenType type, string value, Location location)
    {
        Type = type;
        Value = value;
        Location = location;
    }
}