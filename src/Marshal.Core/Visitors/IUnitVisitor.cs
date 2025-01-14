using Marshal.Core.Syntax;

namespace Marshal.Core.Visitors;

public interface IUnitVisitor : IVisitor
{
    void Visit(CompilationUnit unit);
}