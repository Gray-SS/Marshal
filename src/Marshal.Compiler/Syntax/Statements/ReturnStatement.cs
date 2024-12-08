namespace Marshal.Compiler.Syntax.Statements;

public class ReturnStatement : SyntaxStatement
{
    public Token ReturnKeyword { get; }
    public SyntaxExpression ReturnExpr { get; }    

    public ReturnStatement(Token returnKeyword, SyntaxExpression returnExpr)
    {
        ReturnExpr = returnExpr;
        ReturnKeyword = returnKeyword;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}