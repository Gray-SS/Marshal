namespace Marshal.Compiler.Syntax;

public abstract class SyntaxStatement : SyntaxNode
{
    protected SyntaxStatement(Location loc) : base(loc)
    {
    }
}