namespace Marshal.Compiler.Types;

public abstract class MarshalType : IEquatable<MarshalType>
{
    public string Name { get; }

    public abstract MarshalTypeKind Kind { get; }
    public abstract MarshalType Primitive { get; }

    public static readonly MarshalType Short = new MarshalPrimitiveType("short");
    public static readonly MarshalType Int = new MarshalPrimitiveType("int");
    public static readonly MarshalType Long = new MarshalPrimitiveType("long");
    public static readonly MarshalType Byte = new MarshalPrimitiveType("byte");
    public static readonly MarshalType Char = new MarshalPrimitiveType("char");
    public static readonly MarshalType Void = new MarshalPrimitiveType("void");
    public static readonly MarshalType String = new MarshalPointerType(Char);

    public MarshalType(string name)
    {
        Name = name;
    }

    public bool Equals(MarshalType? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (Primitive == this || other.Primitive == other)
            return Name == other.Name && Kind == other.Kind;

        return Name == other.Name &&
               Kind == other.Kind &&
               Equals(Primitive, other.Primitive);
    }


    public override bool Equals(object? obj)
    {
        return obj is MarshalType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Kind, Primitive);
    }

    public static bool operator ==(MarshalType? left, MarshalType? right)
    {
        if (left is null)
            return right is null;

        return left.Equals(right);
    }

    public static bool operator !=(MarshalType? left, MarshalType? right)
    {
        return !(left == right);
    }
}