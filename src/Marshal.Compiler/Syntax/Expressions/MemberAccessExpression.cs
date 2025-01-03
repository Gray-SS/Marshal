namespace Marshal.Compiler.Syntax.Expressions;

public class MemberAccessExpression : SyntaxExpression
{
    public string MemberName { get; }
    public SyntaxExpression VarExpr { get; }

    public int MemberIdx { get; set; }

    public override ValueCategory ValueCategory => ValueCategory.Locator;

    public MemberAccessExpression(Location loc, SyntaxExpression varExpr, string accessorName) : base(loc)
    {
        VarExpr = varExpr;
        MemberName = accessorName;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(MemberAccessExpression)}] {VarExpr}.{MemberName}", level);
    }
}