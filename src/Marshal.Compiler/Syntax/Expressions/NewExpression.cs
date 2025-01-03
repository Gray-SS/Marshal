namespace Marshal.Compiler.Syntax.Expressions;

public abstract class NewExpression : SyntaxExpression
{
    public Token TypeName { get; }

    public NewExpression(Location loc, Token typeName) : base(loc)
    {
        TypeName = typeName;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class NewStructExpression : NewExpression
{
    public List<SyntaxExpression> Arguments { get; }

    public override ValueCategory ValueCategory => ValueCategory.Transient;

    public NewStructExpression(Location loc, Token typeName, List<SyntaxExpression> arguments) : base(loc, typeName)
    {
        Arguments = arguments;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(NewStructExpression)}:{TypeName.Value}]", level);
        foreach (var arg in Arguments)
        {
            arg.Dump(level + 1);
        }
    }
}

public class NewArrayExpression : NewExpression
{
    public SyntaxExpression LengthExpr { get; }
    public override ValueCategory ValueCategory => ValueCategory.Transient;

    public NewArrayExpression(Location loc, Token typeName, SyntaxExpression lengthExpr) : base(loc, typeName)
    {
        LengthExpr = lengthExpr;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(NewArrayExpression)}]", level);
        LengthExpr.Dump(level + 1);
    }
}