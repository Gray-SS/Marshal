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

    public VarDeclStatement(Location loc, Token nameIdentifier, SyntaxTypeNode syntaxType, SyntaxExpression? initExpression) : base(loc)
    {
        SyntaxType = syntaxType;
        NameToken = nameIdentifier;
        Initializer = initExpression;
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(VarDeclStatement)}:{VarName}]: {SyntaxType.DisplayName}", level);
        Initializer?.Dump(level + 1);
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}