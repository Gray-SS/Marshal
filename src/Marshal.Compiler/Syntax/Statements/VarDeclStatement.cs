using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Statements;

public class VarDeclStatement : SyntaxStatement
{
    public Token NameToken { get; }
    public string VarName => NameToken.Value;

    public MarshalType BoundType { get; set; } = null!;
    public SyntaxTypeNode SyntaxType { get; }
    public SyntaxExpression? Initializer { get; }

    public VariableSymbol Symbol { get; set; } = null!;

    public VarDeclStatement(Token nameIdentifier, SyntaxTypeNode syntaxType, SyntaxExpression? initExpression)
    {
        SyntaxType = syntaxType;
        NameToken = nameIdentifier;
        Initializer = initExpression;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}