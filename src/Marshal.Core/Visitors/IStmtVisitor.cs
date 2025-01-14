using Marshal.Core.Syntax.Statements;

namespace Marshal.Core.Visitors;

public interface IStmtVisitor : IVisitor
{
    void Visit(IfStatement stmt);

    void Visit(WhileStatement stmt);

    void Visit(IncrementStatement stmt);

    void Visit(AssignmentStatement stmt);

    void Visit(ScopeStatement stmt);

    void Visit(FunCallStatement stmt);

    void Visit(FuncDeclStatement stmt);

    void Visit(VarDeclStatement stmt);

    void Visit(StructDeclStatement stmt);

    void Visit(FieldDeclStatement stmt);

    void Visit(ReturnStatement stmt);
}
