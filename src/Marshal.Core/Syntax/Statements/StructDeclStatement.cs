using Marshal.Core.Semantics;
using Marshal.Core.Visitors;

namespace Marshal.Core.Syntax.Statements;

public class StructDeclStatement : SyntaxStatement
{
    public Token Identifier { get; }
    public List<FieldDeclStatement> Fields { get; }

    public StructType Symbol { get; set; } = null!;

    public StructDeclStatement(Location loc, Token identifier, List<FieldDeclStatement> fields) : base(loc)
    {
        Fields = fields;
        Identifier = identifier;
    }

    public override void Accept(IStmtVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override IEnumerable<ISyntaxNode> GetChildren()
    {
        return Fields;
    }
}