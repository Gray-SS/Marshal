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
}

public abstract class MarshalType : Symbol
{
    public int SizeInBytes { get; }
    public bool IsIndexable => IsArray || IsPointer;
    public virtual bool IsNumeric => false;
    public virtual bool IsBoolean => false;
    public virtual bool IsArray => false;
    public virtual bool IsPointer => false;

    public abstract MarshalTypeKind Kind { get; }
    public abstract PrimitiveType Base { get; }

    public static readonly PrimitiveType Byte = new("byte", true, false, 1);
    public static readonly PrimitiveType Short = new("short", true, false, 2);
    public static readonly PrimitiveType Int = new("int", true, false, 4);
    public static readonly PrimitiveType Long = new("long", true, false, 8);
    public static readonly PrimitiveType Char = new("char", false, false, 1);
    public static readonly PrimitiveType Void = new("void", false, false, 0);
    public static readonly PrimitiveType Boolean = new("bool", false, true, 1);
    public static readonly PointerType String = CreatePointer(Char);

    public override SymbolType Type => SymbolType.Type;

    protected MarshalType(string name, int sizeInBytes) : base(name)
    {
        SizeInBytes = sizeInBytes;
    }

    public static PointerType CreatePointer(MarshalType type)
        => new(type);

    public static ArrayType CreateArray(MarshalType type, int length)
        => new(type, length);
}

public class PrimitiveType : MarshalType
{
    public override bool IsNumeric { get; }
    public override bool IsBoolean { get; }

    public override MarshalTypeKind Kind => MarshalTypeKind.Primitive;
    public override PrimitiveType Base { get;}

    public PrimitiveType(string name, bool isNumerics, bool isBoolean, int sizeInBytes) : base(name, sizeInBytes) 
    {
        Base = this;
        IsNumeric = isNumerics;
        IsBoolean = isBoolean;
    }
}

public class PointerType : MarshalType
{
    public MarshalType Pointee { get; }
    public override bool IsPointer => true;

    public override MarshalTypeKind Kind => MarshalTypeKind.Pointer;
    public override PrimitiveType Base => Pointee.Base;


    public PointerType(MarshalType pointee) : base($"{pointee.Name} *", Long.SizeInBytes)
    {
        Pointee = pointee;
    }
}

public class ArrayType : MarshalType
{
    public int ElementCount { get; }
    public MarshalType ElementType { get; }
    public override bool IsArray => true;

    public override MarshalTypeKind Kind => MarshalTypeKind.Array;
    public override PrimitiveType Base => ElementType.Base;

    public ArrayType(MarshalType elementType, int elementCount) : base($"{elementType.Name}[{elementCount}]", elementType.SizeInBytes * elementCount)
    {
        ElementCount = elementCount;
        ElementType = elementType;
    }
}