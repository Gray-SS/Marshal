using Marshal.Compiler.Semantics;
using Marshal.Compiler.Types;

namespace Marshal.Compiler.Syntax.Statements;

public class VarDeclStatement : SyntaxStatement
{
    public Token NameToken { get; }
    public string VarName => NameToken.Value;

    public MarshalType VarType { get; }
    public SyntaxExpression? Initializer { get; }

    public VariableSymbol Symbol { get; set; } = null!;

    public VarDeclStatement(Token nameIdentifier, MarshalType varType, SyntaxExpression? initExpression)
    {
        VarType = varType;
        NameToken = nameIdentifier;
        Initializer = initExpression;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}