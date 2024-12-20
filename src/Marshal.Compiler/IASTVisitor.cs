using Marshal.Compiler.Syntax;
using Marshal.Compiler.Syntax.Expressions;
using Marshal.Compiler.Syntax.Statements;

namespace Marshal.Compiler;

public interface IASTVisitor
{
    void Visit(CompilationUnit unit);

    void Visit(IfStatement stmt);

    void Visit(WhileStatement stmt);

    void Visit(IncrementStatement stmt);

    void Visit(AssignmentStatement stmt);

    void Visit(ScopeStatement stmt);

    void Visit(FunCallStatement stmt);

    void Visit(FuncDeclStatement stmt);

    void Visit(ReturnStatement stmt);

    void Visit(VarDeclStatement stmt);
    
    void Visit(CastExpression expr); 

    void Visit(UnaryOpExpression expr);

    void Visit(BracketExpression expr);

    void Visit(BinaryOpExpression expr);

    void Visit(FunCallExpression expr);

    void Visit(LiteralExpression expr);

    void Visit(VarRefExpression expr);

    void Visit(NewExpression expr);

    void Visit(ArrayInitExpression expr);

    void Visit(ArrayAccessExpression expr);
}