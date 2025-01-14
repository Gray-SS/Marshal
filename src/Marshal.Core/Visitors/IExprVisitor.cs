using Marshal.Core.Syntax.Expressions;
using Marshal.Core.Syntax.Statements;

namespace Marshal.Core.Visitors;

public interface IExprVisitor : IVisitor
{    
    void Visit(CastExpression expr); 

    void Visit(UnaryOpExpression expr);

    void Visit(BracketExpression expr);

    void Visit(BinaryOpExpression expr);

    void Visit(FunCallExpression expr);

    void Visit(LiteralExpression expr);

    void Visit(VarRefExpression expr);

    void Visit(NewExpression expr);

    void Visit(ArrayInitExpression expr);

    void Visit(MemberAccessExpression expr);

    void Visit(ArrayAccessExpression expr);
}