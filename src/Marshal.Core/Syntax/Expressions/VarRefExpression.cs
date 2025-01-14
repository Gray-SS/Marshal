using Marshal.Core.Semantics;
using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Expressions;

public class VarRefExpression : SyntaxExpression
{
    public Token NameToken { get; }

    public VariableSymbol Symbol { get; set; } = null!;

    public override ValueCategory ValueCategory => ValueCategory.Locator;

    public VarRefExpression(Location loc, Token nameIdentifier) : base(loc)
    {
        NameToken = nameIdentifier;
    }
    
    public override void Accept(IExprVisitor visitor)
    {
        visitor.Visit(this);
    }
}