namespace Marshal.Compiler.Syntax.Statements;

public enum BinOpType
{
    Addition,
    Subtraction,
    Multiplication,
    Division,
    Modulo,

    Equals,
    NotEquals,
    BiggerThan,
    BiggerThanEq,
    LessThan,
    LessThanEq,
}

public sealed class BinaryOpExpression : SyntaxExpression
{
    public SyntaxExpression Left { get; }

    public SyntaxExpression Right { get; }

    public BinOpType Operation { get; }

    public override ValueCategory ValueCategory => ValueCategory.Transient;

    public BinaryOpExpression(Location loc, SyntaxExpression left, SyntaxExpression right, BinOpType opType) : base(loc)
    {
        Left = left;
        Right = right;
        Operation = opType;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(BinaryOpExpression)}:{Operation}]", level);
        Left.Dump(level + 1);
        Right.Dump(level + 1);
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}