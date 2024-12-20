using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Expressions;

public class FunCallExpression : SyntaxExpression
{
    public Token NameToken { get; }

    public List<SyntaxExpression> Args { get; }

    public FunctionSymbol Symbol { get; set; } = null!;

    public override ValueCategory ValueCategory => ValueCategory.Transient;

    public FunCallExpression(Location loc, Token nameIdentifier, List<SyntaxExpression> parameters) : base(loc)
    {
        Args = parameters;
        NameToken = nameIdentifier;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(FunCallExpression)}:{NameToken.Value}]", level);
        foreach (SyntaxExpression expr in Args)
            expr.Dump(level + 1);
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}