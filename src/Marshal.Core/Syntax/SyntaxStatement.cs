using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax;

public abstract class SyntaxStatement : SyntaxNode<IStmtVisitor>
{
    protected SyntaxStatement(Location loc) : base(loc)
    {
    }
}