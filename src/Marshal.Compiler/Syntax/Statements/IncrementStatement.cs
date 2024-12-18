using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Statements;

public class IncrementStatement : SyntaxStatement
{
    public bool Decrement { get; }
    public Token NameToken { get; }
    public VariableSymbol Symbol { get; set; } = null!;

    public IncrementStatement(Token nameToken, bool decrement)
    {
        NameToken = nameToken;
        Decrement = decrement;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}