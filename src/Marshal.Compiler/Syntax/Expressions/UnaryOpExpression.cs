namespace Marshal.Compiler.Syntax.Expressions;

public enum UnaryOpType
{
    Negation,
    Not,
    AddressOf,
    Deference,
}

public class UnaryOpExpression : SyntaxExpression
{
    public SyntaxExpression Operand { get; }
    public UnaryOpType Operation { get; }

    public override ValueCategory ValueCategory => Operation == UnaryOpType.Deference ? ValueCategory.Locator : ValueCategory.Transient;

    public UnaryOpExpression(Location loc, SyntaxExpression operand, UnaryOpType operation) : base(loc)
    {
        Operand = operand;
        Operation = operation;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(UnaryOpExpression)}:{Operation}]", level);
        Operand.Dump(level + 1);
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}