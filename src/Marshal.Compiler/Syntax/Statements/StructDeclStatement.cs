using Marshal.Compiler.Semantics;

namespace Marshal.Compiler.Syntax.Statements;

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

    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override void Dump(int level = 0)
    {
        Dump($"[{nameof(StructDeclStatement)}] {Identifier.Value}", level);
        foreach (var field in Fields)
        {
            field.Dump(level + 1);
        }
    }
}