namespace Marshal.Compiler.Syntax;

public abstract class TypeNode
{
    public abstract string BaseTypeName { get; }
}

public class BaseTypeNode : TypeNode
{
    public string Name => Identifier.Value;

    public override string BaseTypeName => Name;

    public Token Identifier { get; }
    
    public BaseTypeNode(Token identifier)
    {
        Identifier = identifier;
    }
}

public class PointerTypeNode : TypeNode
{
    public TypeNode Pointee { get; }

    public override string BaseTypeName => Pointee.BaseTypeName;

    public PointerTypeNode(TypeNode pointee)
    {
        Pointee = pointee;
    }
}

public class ArrayTypeNode : TypeNode
{
    public TypeNode ElementType { get; }
    
    public Token? ElementCount { get; }

    public override string BaseTypeName => ElementType.BaseTypeName;

    public ArrayTypeNode(TypeNode elementType, Token? elementCount)
    {
        ElementType = elementType;
        ElementCount = elementCount;
    }
}