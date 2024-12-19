namespace Marshal.Compiler.Syntax;

public abstract class SyntaxNode
{
    public Location Loc { get; }

    public SyntaxNode(Location loc)
    {
        Loc = loc;
    }

    public abstract void Accept(IASTVisitor visitor);
}