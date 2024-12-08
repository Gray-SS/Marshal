namespace Marshal.Compiler.Syntax.Statements;

public enum BinOperatorType
{
    Addition,
    Subtraction,
    Multiplication,
    Division,
}

public sealed class BinaryOpExpression : SyntaxExpression
{
    public SyntaxExpression Left { get; }

    public SyntaxExpression Right { get; }

    public BinOperatorType OpType { get; }

    public BinaryOpExpression(SyntaxExpression left, SyntaxExpression right, BinOperatorType opType)
    {
        Left = left;
        Right = right;
        OpType = opType;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}