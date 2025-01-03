using System.Diagnostics.CodeAnalysis;
namespace Marshal.Compiler.Semantics;

public enum SymbolType
{
    Type,
    Function,
    Variable,
    Field,
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

public class FieldSymbol : Symbol
{
    public MarshalType DataType { get; set; } = null!;

    public override SymbolType Type => SymbolType.Field;

    public FieldSymbol(string name, MarshalType dataType) : base(name)
    {
        DataType = dataType;
    }
}

public enum MarshalTypeKind : byte
{
    Primitive,
    Pointer,
    Array,
    Alias,
    String,
    UserDefined,
}

public enum CastOperation : byte
{
    Invalid,
    Identity,
    Bitcast,
    Truncate,
    ZeroExtend,
    SignExtend,
    Float2SInt,
    Float2UInt,
    SInt2Float,
    UInt2Float,
    FloatTrunc,
    FloatExt,
}

public enum CastKind : byte
{
    Invalid = 0,
    Explicit,
    Implicit,
}

public abstract class MarshalType : Symbol
{
    public abstract int SizeInBytes { get; }
    public bool IsIndexable => IsArray || IsPointer || IsString;
    public virtual bool IsString => false;
    public virtual bool IsNumeric => false;
    public virtual bool IsBoolean => false;
    public virtual bool IsArray => false;
    public virtual bool IsPointer => false;
    public virtual bool IsReferenced => false;
    public virtual bool IsSigned => false;
    public virtual bool IsFloating => false;
    public bool IsValueType => !IsReferenced;

    public abstract MarshalTypeKind Kind { get; }
    public abstract MarshalType Base { get; }

    public static readonly PrimitiveType Byte = new("byte", false, false, true, false, 1);
    public static readonly PrimitiveType Short = new("short", true, false, true, false, 2);
    public static readonly PrimitiveType Int = new("int", true, false, true, false, 4);
    public static readonly PrimitiveType Long = new("long", true, false, true, false, 8);
    public static readonly PrimitiveType Float = new("float", true, true, true, false, 4);
    public static readonly PrimitiveType Char = new("char", false, false, true, false, 1);
    public static readonly PrimitiveType Void = new("void", false, false, false, false, 0);
    public static readonly PrimitiveType Boolean = new("bool", false, false, false, true, 1);
    public static readonly StringType String = new();

    public override SymbolType Type => SymbolType.Type;

    protected MarshalType(string name) : base(name)
    {
    }

    public abstract CastKind GetCastKind(MarshalType targetType);
    public abstract CastOperation GetCastOperation(MarshalType targetType);

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
    public override MarshalType Base => Aliased.Base;

    public TypeAlias(string name, MarshalType aliased) : base(name)
    {
        Aliased = aliased;
    }

    public override CastKind GetCastKind(MarshalType targetType)
    {
        return Aliased.GetCastKind(targetType);
    }

    public override CastOperation GetCastOperation(MarshalType targetType)
    {
        return Aliased.GetCastOperation(targetType);
    }
}

public class PrimitiveType : MarshalType
{
    public override bool IsNumeric { get; }
    public override bool IsBoolean { get; }
    public override bool IsSigned { get; }
    public override bool IsFloating { get; }
    public override int SizeInBytes { get; }

    public override MarshalTypeKind Kind => MarshalTypeKind.Primitive;
    public override MarshalType Base { get; }

    public PrimitiveType(string name, bool isSigned, bool isFloating, bool isNumerics, bool isBoolean, int sizeInBytes) : base(name) 
    {
        Base = this;
        IsNumeric = isNumerics;
        IsBoolean = isBoolean;
        IsFloating = isFloating;
        IsSigned = isSigned;
        SizeInBytes = sizeInBytes;
    }

    public override CastKind GetCastKind(MarshalType targetType)
    {
        if (targetType is TypeAlias alias)
            return GetCastKind(alias.Aliased);

        if (targetType.IsPointer)
            return CastKind.Invalid;

        if (SizeInBytes <= targetType.SizeInBytes)
            return CastKind.Implicit;

        return CastKind.Explicit;
    }

    public override CastOperation GetCastOperation(MarshalType targetType)
    {
        if (targetType == this)
            return CastOperation.Identity;

        if (targetType is TypeAlias alias)
            return GetCastOperation(alias.Aliased);

        if (IsNumeric && targetType.IsNumeric)
        {
            if (SizeInBytes > targetType.SizeInBytes)
                return CastOperation.Truncate;
            if (SizeInBytes < targetType.SizeInBytes)
                return IsSigned ? CastOperation.SignExtend : CastOperation.ZeroExtend;
            return CastOperation.Identity;
        }

        if (IsNumeric && targetType.IsFloating)
            return IsSigned ? CastOperation.SInt2Float : CastOperation.UInt2Float;

        if (IsFloating && targetType.IsNumeric)
            return IsSigned ? CastOperation.Float2SInt : CastOperation.Float2UInt;

        if (IsFloating && targetType.IsFloating)
        {
            if (SizeInBytes > targetType.SizeInBytes)
                return CastOperation.FloatTrunc;
            else
                return CastOperation.FloatExt;
        }

        return CastOperation.Invalid;
    }
}

public class StringType : MarshalType
{
    private static readonly PointerType CharPointerType = new(Char);
    
    public override bool IsString => true;
    public override bool IsReferenced => true;
    public override int SizeInBytes => CharPointerType.SizeInBytes;

    public override MarshalTypeKind Kind => MarshalTypeKind.String;
    public override MarshalType Base => Char.Base;

    public StringType() : base("string") { }

    public override CastKind GetCastKind(MarshalType targetType)
    {
        if (targetType is TypeAlias alias)
            return GetCastKind(alias.Aliased);

        if (targetType is StringType)
            return CastKind.Implicit;

        return CastKind.Invalid;
    }

    public override CastOperation GetCastOperation(MarshalType targetType)
    {
        if (targetType == this)
            return CastOperation.Identity;

        if (targetType is TypeAlias alias)
            return GetCastOperation(alias.Aliased);

        return CastOperation.Invalid;
    }
}

public class PointerType : MarshalType
{
    public MarshalType Pointee { get; }
    public override bool IsPointer => true;
    public override int SizeInBytes => Long.SizeInBytes;

    public override MarshalTypeKind Kind => MarshalTypeKind.Pointer;
    public override MarshalType Base => Pointee.Base;

    public PointerType(MarshalType pointee) : base($"{pointee.Name}*")
    {
        Pointee = pointee;
    }

    public override CastKind GetCastKind(MarshalType targetType)
    {
        if (targetType is TypeAlias alias)
            return GetCastKind(alias.Aliased);

        if (!targetType.IsPointer)
            return CastKind.Invalid;

        return CastKind.Implicit;
    }

    public override CastOperation GetCastOperation(MarshalType targetType)
    {
        if (targetType is TypeAlias alias)
            return GetCastOperation(alias.Aliased);

        if (targetType == this)
            return CastOperation.Identity;

        if (targetType.IsPointer)
        {
            PointerType targetPointer = (PointerType)targetType;
            if (Pointee == targetPointer.Pointee)
                return CastOperation.Identity;

            return CastOperation.Bitcast;
        }

        return CastOperation.Invalid;
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
    public override MarshalType Base => ElementType.Base;

    public ArrayType(MarshalType elementType) : base($"{elementType.Name}[]")
    {
        ElementType = elementType;
    }

    public override CastKind GetCastKind(MarshalType targetType)
    {
        if (targetType is TypeAlias alias)
            return GetCastKind(alias.Aliased);

        if (!targetType.IsArray)
            return CastKind.Invalid;

        ArrayType otherArr = (ArrayType)targetType;
        if (ElementType == otherArr.ElementType)
            return CastKind.Implicit;

        return CastKind.Invalid;
    }

    public override CastOperation GetCastOperation(MarshalType targetType)
    {
        if (targetType is TypeAlias alias)
            return GetCastOperation(alias.Aliased);

        if (!targetType.IsArray)
            return CastOperation.Invalid;

        ArrayType targetArray = (ArrayType)targetType;

        if (ElementType == targetArray.ElementType)
            return CastOperation.Identity;

        return CastOperation.Invalid;
    }
}

public class StructType : MarshalType
{
    public FieldSymbol[] Fields { get; }
    
    public override int SizeInBytes { get; }
    public override MarshalTypeKind Kind => MarshalTypeKind.UserDefined;

    public override MarshalType Base => this;

    public StructType(
        string name,
        int sizeInBytes,
        FieldSymbol[] fields) : base(name)
    {
        SizeInBytes = sizeInBytes;
        Fields = fields;
    }

    public int GetFieldIndex(string memberName)
    {
        for (int i = 0; i < Fields.Length; i++)
        {
            if (Fields[i].Name == memberName)
                return i;
        }

        return -1;
    }

    public bool TryGetField(string memberName, [NotNullWhen(true)] out FieldSymbol? field)
    {
        foreach (FieldSymbol f in Fields)
        {
            if (f.Name == memberName)
            {
                field = f;
                return true;
            }
        }

        field = null;
        return false;
    }

    public override CastKind GetCastKind(MarshalType targetType)
    {
        if (targetType is TypeAlias alias)
            return GetCastKind(alias.Aliased);

        if (targetType is StructType userDefinedType)
        {
            if (this == userDefinedType)
                return CastKind.Implicit;
        }

        return CastKind.Invalid;
    }

    public override CastOperation GetCastOperation(MarshalType targetType)
    {
        if (targetType is TypeAlias alias)
            return GetCastOperation(alias.Aliased);

        if (targetType is StructType userDefinedType)
        {
            if (this == userDefinedType)
                return CastOperation.Identity;
        }

        return CastOperation.Invalid;
    }
}