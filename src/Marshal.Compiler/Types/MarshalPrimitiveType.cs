namespace Marshal.Compiler.Types;

public class MarshalPrimitiveType : MarshalType
{
    public override MarshalTypeKind Kind => MarshalTypeKind.Primitive;
    public override MarshalType Primitive => this;

    public MarshalPrimitiveType(string name) : base(name)
    {
    }
}
