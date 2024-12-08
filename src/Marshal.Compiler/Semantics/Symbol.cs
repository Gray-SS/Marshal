using System.Diagnostics.CodeAnalysis;
using Marshal.Compiler.Types;

namespace Marshal.Compiler.Semantics;

public enum SymbolType
{
    Function,
    Variable,
    Type,
    Param,
}

public abstract class Symbol : IEquatable<Symbol>
{
    public string Name { get; }
    public abstract SymbolType Type { get; }

    public static readonly TypeSymbol Byte = new("byte");
    public static readonly TypeSymbol Short = new("short");
    public static readonly TypeSymbol Int = new("int");
    public static readonly TypeSymbol Long = new("long");
    public static readonly TypeSymbol Char = new("char");
    public static readonly TypeSymbol Void = new("void");

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

    public bool IsDefined { get; }


    public override SymbolType Type => SymbolType.Function;

    public FunctionSymbol(string name, bool isExtern, bool isDefined, List<VariableSymbol> parameters) : base(name)
    {
        IsExtern = isExtern;
        IsDefined = isDefined;
        Params = parameters;
    }
}

public class VariableSymbol : Symbol
{
    public bool IsInitialized { get; set; }
    public MarshalType DataType { get; set; } = null!;

    public override SymbolType Type => SymbolType.Variable;

    public VariableSymbol(string name, bool init) : base(name)
    {
        IsInitialized = init;
    }
}

public class TypeSymbol : Symbol
{
    public override SymbolType Type => SymbolType.Type;

    public TypeSymbol(string name) : base(name)
    {
    }
}