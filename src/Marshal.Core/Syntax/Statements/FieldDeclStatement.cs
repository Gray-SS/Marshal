using Marshal.Core.Semantics;
using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Statements;

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

    public override void Accept(IStmtVisitor visitor)
    {
        visitor.Visit(this);
    }
}