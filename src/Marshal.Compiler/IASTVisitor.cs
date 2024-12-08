using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;

namespace Marshal.Compiler;

public interface IASTVisitor
{
    void Visit(CompilationUnit unit);

    void Visit(AssignmentStatement stmt);

    void Visit(ScopeStatement stmt);

    void Visit(FunCallStatement stmt);

    void Visit(FuncDeclStatement stmt);

    void Visit(BinaryOpExpression stmt);

    void Visit(ReturnStatement stmt);

    void Visit(VarDeclStatement stmt);

    void Visit(FunCallExpression expr);

    void Visit(LiteralExpression expr);

    void Visit(VarRefExpression expr);

    void Visit(ArrayInitExpression expr);
}