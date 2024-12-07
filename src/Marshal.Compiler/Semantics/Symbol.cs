using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Reflection.Metadata;
using Swigged.LLVM;

namespace Marshal.Compiler.Semantics;

public enum SymbolType
{
    Function,
    Variable,
    CustomType,
}

public abstract class Symbol : IEquatable<Symbol>
{
    public string Name { get; }
    public abstract SymbolType Type { get; }

    public static readonly TypeSymbol Byte = TypeSymbol.CreatePrimitive("byte", LLVM.Int8Type());
    public static readonly TypeSymbol Short = TypeSymbol.CreatePrimitive("short", LLVM.Int16Type());
    public static readonly TypeSymbol Int = TypeSymbol.CreatePrimitive("int", LLVM.Int32Type());
    public static readonly TypeSymbol Long = TypeSymbol.CreatePrimitive("long", LLVM.Int64Type());
    public static readonly TypeSymbol Char = TypeSymbol.CreatePrimitive("char", LLVM.Int8Type());
    public static readonly TypeSymbol Void = TypeSymbol.CreatePrimitive("void", LLVM.VoidType());
    public static readonly TypeSymbol String = TypeSymbol.CreatePrimitive("string", LLVM.PointerType(LLVM.Int8Type(), 0));

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

public class ParamSymbol
{
    public string Name { get; }
    public TypeSymbol Type { get; }

    public bool IsParams { get; }

    public ParamSymbol(bool isParams)
    {
        Name = null!;
        Type = null!;
        IsParams = isParams;
    }

    public ParamSymbol(string name, TypeSymbol type)
    {
        Name = name;
        Type = type;
    }
}

public class FunctionSymbol : Symbol
{
    public TypeSymbol ReturnType { get; }

    public bool IsExtern { get; }

    public bool IsDefined { get; }

    public List<ParamSymbol> Params { get; }

    public override SymbolType Type => SymbolType.Function;

    public FunctionSymbol(string name, TypeSymbol returnType, bool isExtern, bool isDefined, List<ParamSymbol> parameters) : base(name)
    {
        IsExtern = isExtern;
        ReturnType = returnType;
        IsDefined = isDefined;
        Params = parameters;
    }
}

public class VariableSymbol : Symbol
{
    public bool IsInitialized { get; set; }
    public TypeSymbol VariableType { get; }

    public override SymbolType Type => SymbolType.Variable;

    public VariableSymbol(string name, TypeSymbol varType, bool init) : base(name)
    {
        IsInitialized = init;
        VariableType = varType;
    }
}

public class TypeSymbol : Symbol
{
    public bool IsPrimitive { get; }

    public bool IsArray { get; }

    public TypeRef? LLVMType { get; }

    public TypeSymbol? ElementType { get; }

    public int? ArraySize { get; }

    public override SymbolType Type => SymbolType.CustomType;

    private TypeSymbol(string name, TypeRef? llvmType, bool isPrimitive, bool isArray = false, TypeSymbol? elementType = null, int? arraySize = null) 
        : base(name)
    {
        LLVMType = llvmType;
        IsPrimitive = isPrimitive;
        IsArray = isArray;
        ElementType = elementType;
        ArraySize = arraySize;
    }

public static TypeSymbol CreateCustom(string name)
    {
        return new TypeSymbol(name, null, false);
    }

    public static TypeSymbol CreatePrimitive(string name, TypeRef llvmType)
    {
        return new TypeSymbol(name, llvmType, true);
    }

    public static TypeSymbol CreateArray(TypeSymbol elementType, int? arraySize = null)
    {
        if (elementType.IsArray)
        {
            throw new InvalidOperationException("Nested arrays are not supported.");
        }

        var llvmType = arraySize.HasValue
            ? LLVM.ArrayType(elementType.LLVMType!.Value, (uint)arraySize.Value)
            : LLVM.PointerType(elementType.LLVMType!.Value, 0);

        var name = arraySize.HasValue 
            ? $"{elementType.Name}[{arraySize.Value}]"
            : $"{elementType.Name}[]";

        return new TypeSymbol(name, llvmType, false, true, elementType, arraySize);
    }
}