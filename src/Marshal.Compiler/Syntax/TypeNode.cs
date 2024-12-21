namespace Marshal.Compiler.Syntax;

public abstract class SyntaxTypeNode
{
    public string Name { get; }
    public string BaseName => BaseType.Name;
    public abstract SyntaxTypeNode BaseType { get; }

    protected SyntaxTypeNode(string displayName)
    {
        Name = displayName;
    }
}

public class SyntaxPrimitiveType : SyntaxTypeNode
{
    public override SyntaxTypeNode BaseType { get; }

    public SyntaxPrimitiveType(string name) : base(name)
    {
        BaseType = this;
    }
}

public class SyntaxPointerType : SyntaxTypeNode
{
    public SyntaxTypeNode Pointee { get; }
    public override SyntaxTypeNode BaseType => Pointee.BaseType;

    public SyntaxPointerType(SyntaxTypeNode pointee) : base($"{pointee.Name}*")
    {
        Pointee = pointee;
    }
}

public class SyntaxArrayType : SyntaxTypeNode
{
    public SyntaxTypeNode ElementType { get; }
    public override SyntaxTypeNode BaseType => ElementType.BaseType;

    public SyntaxArrayType(SyntaxTypeNode elementType) : base($"{elementType.Name}[]")
    {
        ElementType = elementType;
    }
}