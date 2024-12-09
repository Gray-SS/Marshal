using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Statements;

public class FunCallStatement : SyntaxStatement
{
    public Token NameToken { get; }

    public List<SyntaxExpression> Args { get; }

    public FunctionSymbol Symbol { get; set; } = null!;

    public FunCallStatement(Token nameIdentifier, List<SyntaxExpression> parameters)
    {
        Args = parameters;
        NameToken = nameIdentifier;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}