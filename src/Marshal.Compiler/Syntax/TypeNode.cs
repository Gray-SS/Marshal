namespace Marshal.Compiler.Syntax;

public abstract class SyntaxTypeNode
{
    public string Name => NameToken.Value;

    public Location Location => NameToken.Loc;

    public abstract Token NameToken { get; }

    public abstract SyntaxTypeNode BaseType { get; }
}

public class SyntaxPrimitiveType : SyntaxTypeNode
{
    public override Token NameToken { get; }
    public override SyntaxTypeNode BaseType { get; }

    public SyntaxPrimitiveType(Token nameToken)
    {
        BaseType = this;
        NameToken = nameToken;
    }
}

public class SyntaxPointerType : SyntaxTypeNode
{
    public SyntaxTypeNode Pointee { get; }

    public override Token NameToken => Pointee.NameToken;
    public override SyntaxTypeNode BaseType => Pointee.BaseType;

    public SyntaxPointerType(SyntaxTypeNode pointee)
    {
        Pointee = pointee;
    }
}

public class SyntaxArrayType : SyntaxTypeNode
{
    public SyntaxTypeNode ElementType { get; }

    public override Token NameToken => ElementType.NameToken;
    public override SyntaxTypeNode BaseType => ElementType.BaseType;

    public SyntaxArrayType(SyntaxTypeNode elementType)
    {
        ElementType = elementType;
    }
}