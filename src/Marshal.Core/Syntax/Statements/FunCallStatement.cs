using Marshal.Core.Semantics;
using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Statements;

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
    
    public override void Accept(IStmtVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        return Args;
    }
}