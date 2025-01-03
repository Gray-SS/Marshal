namespace Marshal.Compiler.Syntax.Statements;

public class ReturnStatement : SyntaxStatement
{
    public Token ReturnKeyword { get; }
    public SyntaxExpression? ReturnExpr { get; }    

    public ReturnStatement(Location loc, Token returnKeyword, SyntaxExpression? returnExpr) : base(loc)
    {
        ReturnExpr = returnExpr;
        ReturnKeyword = returnKeyword;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(ReturnStatement)}]", level);
        ReturnExpr?.Dump(level + 1);
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}