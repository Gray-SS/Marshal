namespace Marshal.Compiler.Types;

public class MarshalArrayType : MarshalType
{
    public int ElementCount { get; }

    public MarshalType ElementType { get; }

    public override MarshalTypeKind Kind => MarshalTypeKind.Array;

    public override MarshalType Primitive => ElementType.Primitive;

    public MarshalArrayType(MarshalType elementType, int elementCount) : base($"{elementType.Name}[{elementCount}]")
    {
        ElementCount = elementCount;
        ElementType = elementType;
    }
}