using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Statements;

public class ReturnStatement : SyntaxStatement
{
    public Token ReturnKeyword { get; }
    public SyntaxExpression? ReturnExpr { get; }    

    public ReturnStatement(Location loc, Token returnKeyword, SyntaxExpression? returnExpr) : base(loc)
    {
        ReturnExpr = returnExpr;
        ReturnKeyword = returnKeyword;
    }
    
    public override void Accept(IStmtVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        if (ReturnExpr != null)
            yield return ReturnExpr;
    }
}