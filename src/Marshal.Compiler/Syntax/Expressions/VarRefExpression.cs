using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Expressions;

public class VarRefExpression : SyntaxExpression
{
    public Token NameToken { get; }

    public VariableSymbol Symbol { get; set; } = null!;

    public VarRefExpression(Token nameIdentifier)
    {
        NameToken = nameIdentifier;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}