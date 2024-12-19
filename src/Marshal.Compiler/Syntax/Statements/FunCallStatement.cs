using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Statements;

public class FunCallStatement : SyntaxStatement
{
    public Token NameToken { get; }

    public List<SyntaxExpression> Args { get; }

    public FunctionSymbol Symbol { get; set; } = null!;

    public FunCallStatement(Location loc, Token nameIdentifier, List<SyntaxExpression> parameters) : base(loc)
    {
        Args = parameters;
        NameToken = nameIdentifier;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}