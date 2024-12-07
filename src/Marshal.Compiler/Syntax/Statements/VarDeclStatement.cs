using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Statements;

public class VarDeclStatement : SyntaxStatement
{
    public string VarName => NameIdentifier.Value;

    public TypeSymbol? VarType { get; set; }

    public Token NameIdentifier { get; }

    public Token TypeIdentifier { get; }

    public SyntaxExpression? InitExpression { get; }

    public VarDeclStatement(Token nameIdentifier, Token typeIdentifier, SyntaxExpression? initExpression)
    {
        NameIdentifier = nameIdentifier;
        TypeIdentifier = typeIdentifier;
        InitExpression = initExpression;
    }

    public override void Accept(IVisitor visitor)
    {
        visitor.Visit(this);
    }
}