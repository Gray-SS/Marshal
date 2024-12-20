namespace Marshal.Compiler.Syntax;

public abstract class SyntaxNode
{
    public Location Loc { get; }

    public SyntaxNode(Location loc)
    {
        Loc = loc;
    }

    protected static void Dump(string text, int level)
    {
        for (int i = 0; i < level * 2; i++)
           Console.Write(' ');

        Console.WriteLine(text);
    }   

    public abstract void Dump(int level = 0);

    public abstract void Accept(IASTVisitor visitor);
}