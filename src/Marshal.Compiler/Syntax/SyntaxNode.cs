namespace Marshal.Compiler.Syntax;

public abstract class SyntaxNode
{
    public abstract void Accept(IVisitor visitor);
}