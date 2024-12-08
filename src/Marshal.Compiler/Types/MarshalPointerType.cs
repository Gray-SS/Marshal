namespace Marshal.Compiler.Types;

public class MarshalPointerType : MarshalType
{
    public MarshalType Pointee { get; }

    public override MarshalTypeKind Kind => MarshalTypeKind.Pointer;

    public override MarshalType Primitive => Pointee.Primitive;

    public MarshalPointerType(MarshalType pointee) : base($"{pointee.Name}*")
    {
        Pointee = pointee;
    }
}
