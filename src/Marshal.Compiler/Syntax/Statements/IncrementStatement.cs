using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Statements;

public class IncrementStatement : SyntaxStatement
{
    public bool Decrement { get; }
    public Token NameToken { get; }
    public VariableSymbol Symbol { get; set; } = null!;

    public IncrementStatement(Location loc, Token nameToken, bool decrement) : base(loc)
    {
        NameToken = nameToken;
        Decrement = decrement;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{(Decrement ? "Decrement" : "Increment")}:{NameToken.Value}]", level);
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}