using System.Diagnostics.CodeAnalysis;
namespace Marshal.Compiler.Semantics;

public enum SymbolType
{
    Type,
    Function,
    Variable,
}

public abstract class Symbol : IEquatable<Symbol>
{
    public string Name { get; }
    public abstract SymbolType Type { get; }

    public Symbol(string name)
    {
        Name = name;
    }

    public bool Equals(Symbol? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        return other is not null && other.Name == Name && other.Type == Type;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Symbol symbol && Equals(symbol);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Type);
    }

    public static bool operator ==(Symbol? a, Symbol? b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    public static bool operator !=(Symbol? a, Symbol? b)
        => !(a == b);
}

public class FunctionSymbol : Symbol
{
    public List<VariableSymbol> Params { get; }
    public MarshalType ReturnType { get; set; } = null!;

    public bool IsExtern { get; }
    public bool IsDefined { get; set; }

    public override SymbolType Type => SymbolType.Function;

    public FunctionSymbol(
        string name, 
        bool isExtern, 
        bool isDefined,
        MarshalType returnType, 
        List<VariableSymbol> parameters) : base(name)
    {
        IsExtern = isExtern;
        IsDefined = isDefined;
        ReturnType = returnType;
        Params = parameters;
    }
}

public class VariableSymbol : Symbol
{
    public bool IsInitialized { get; set; }
    public MarshalType DataType { get; set; } = null!;

    public override SymbolType Type => SymbolType.Variable;

    public VariableSymbol(
        string name, 
        MarshalType dataType, 
        bool isInitialized) : base(name)
    {
        IsInitialized = isInitialized;
        DataType = dataType;
    }
}

public enum MarshalTypeKind
{
    Primitive,
    Pointer,
    Array,
    Alias,
}

public abstract class MarshalType : Symbol
{
    public abstract int SizeInBytes { get; }
    public bool IsIndexable => IsArray || IsPointer;
    public virtual bool IsNumeric => false;
    public virtual bool IsBoolean => false;
    public virtual bool IsArray => false;
    public virtual bool IsPointer => false;
    public virtual bool IsReferenced => false;
    public bool IsValueType => !IsReferenced;

    public abstract MarshalTypeKind Kind { get; }
    public abstract PrimitiveType Base { get; }

    public static readonly PrimitiveType Byte = new("byte", true, false, 1);
    public static readonly PrimitiveType Short = new("short", true, false, 2);
    public static readonly PrimitiveType Int = new("int", true, false, 4);
    public static readonly PrimitiveType Long = new("long", true, false, 8);
    public static readonly PrimitiveType Char = new("char", false, false, 1);
    public static readonly PrimitiveType Void = new("void", false, false, 0);
    public static readonly PrimitiveType Boolean = new("bool", false, true, 1);
    public static readonly TypeAlias String = new("string", CreatePointer(Char));

    public override SymbolType Type => SymbolType.Type;

    protected MarshalType(string name) : base(name)
    {
    }

    public static PointerType CreatePointer(MarshalType type)
        => new(type);

    public static ArrayType CreateDynamicArray(MarshalType type)
        => new(type);
}

public class TypeAlias : MarshalType
{
    public MarshalType Aliased { get; }
    public override int SizeInBytes => Aliased.SizeInBytes;

    public override MarshalTypeKind Kind => MarshalTypeKind.Alias;
    public override PrimitiveType Base => Aliased.Base;

    public TypeAlias(string name, MarshalType aliased) : base(name)
    {
        Aliased = aliased;
    }
}

public class PrimitiveType : MarshalType
{
    public override bool IsNumeric { get; }
    public override bool IsBoolean { get; }
    public override int SizeInBytes { get; }

    public override MarshalTypeKind Kind => MarshalTypeKind.Primitive;
    public override PrimitiveType Base { get; }

    public PrimitiveType(string name, bool isNumerics, bool isBoolean, int sizeInBytes) : base(name) 
    {
        Base = this;
        IsNumeric = isNumerics;
        IsBoolean = isBoolean;
        SizeInBytes = sizeInBytes;
    }
}

public class PointerType : MarshalType
{
    public MarshalType Pointee { get; }
    public override bool IsPointer => true;
    public override int SizeInBytes => Long.SizeInBytes;

    public override MarshalTypeKind Kind => MarshalTypeKind.Pointer;
    public override PrimitiveType Base => Pointee.Base;


    public PointerType(MarshalType pointee) : base($"{pointee.Name}*")
    {
        Pointee = pointee;
    }
}

public class ArrayType : MarshalType
{
    public int ElementCount { get; set; }
    public MarshalType ElementType { get; }
    public override bool IsArray => true;
    public override bool IsReferenced => true;
    public override int SizeInBytes => ElementCount * ElementType.SizeInBytes;

    public override MarshalTypeKind Kind => MarshalTypeKind.Array;
    public override PrimitiveType Base => ElementType.Base;

    public ArrayType(MarshalType elementType) : base($"{elementType.Name}[]")
    {
        ElementType = elementType;
    }
}