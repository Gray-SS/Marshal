using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax;

public interface ISyntaxNode
{
    Location Loc { get; }

    void Dump(int level = 0);

    IEnumerable<ISyntaxNode> GetChildren();
}

public abstract class SyntaxNode : ISyntaxNode
{
    public Location Loc { get; }

    public SyntaxNode(Location loc)
    {
        Loc = loc;
    }

    public virtual IEnumerable<ISyntaxNode> GetChildren()
        => [];

    public void Dump(int level = 0)
    {
        for (int i = 0; i < level * 2; i++)
           Console.Write(' ');

        Console.WriteLine($"[{this}]");
        foreach (ISyntaxNode child in GetChildren())
            child.Dump(level + 1);
    }
}

public abstract class SyntaxNode<TVisitor>(Location loc) : SyntaxNode(loc) 
    where TVisitor : IVisitor
{
    public abstract void Accept(TVisitor visitor);
}