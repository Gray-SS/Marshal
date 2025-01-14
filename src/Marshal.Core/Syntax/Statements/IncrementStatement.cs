using Marshal.Core.Semantics;
using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Statements;

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

    public override void Accept(IStmtVisitor visitor)
    {
        visitor.Visit(this);
    }
}