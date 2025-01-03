using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Statements;

public sealed class FieldDeclStatement : SyntaxStatement
{
    public string FieldName { get; set;}
    public SyntaxTypeNode SyntaxType { get; }

    public FieldSymbol Symbol { get; set; } = null!; 

    public FieldDeclStatement(Location loc, string fieldName, SyntaxTypeNode syntaxType) : base(loc)
    {
        FieldName = fieldName;
        SyntaxType = syntaxType;
    }

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(FieldDeclStatement)}]", level);
    }
}