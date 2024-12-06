using System.Diagnostics.CodeAnalysis;

namespace Marshal.Compiler.Semantics;

public readonly struct SymbolKey : IEquatable<SymbolKey>
{
    public readonly string Name;
    public readonly SymbolType Type;

    public SymbolKey(string name, SymbolType type)
    {
        Name = name;
        Type = type;
    }

    public bool Equals(SymbolKey other)
    {
        return other.Name == Name && other.Type == Type;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is SymbolKey key && Equals(key);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Type);
    }

    public static bool operator ==(SymbolKey a, SymbolKey b)
        => a.Equals(b);

    public static bool operator !=(SymbolKey a, SymbolKey b)
        => !a.Equals(b);
}

public class SymbolTable
{
    public IReadOnlyCollection<Symbol> Symbols => _symbols.Values;

    private readonly Dictionary<SymbolKey, Symbol> _symbols;

    public SymbolTable()
    {
        _symbols = new Dictionary<SymbolKey, Symbol>();
    }

    public bool AddSymbol(Symbol symbol)
    {
        var key = new SymbolKey(symbol.Name, symbol.Type);
        return _symbols.TryAdd(key, symbol);
    }

    public void RemoveSymbol(string name, SymbolType type)
    {
        var key = new SymbolKey(name, type);
        _symbols.Remove(key);
    }

    public Symbol? GetSymbol(string name, SymbolType type)
    {
        var key = new SymbolKey(name, type);
        if (_symbols.TryGetValue(key, out Symbol? symbol))
            return symbol;

        return symbol;
    }

    public TypeSymbol GetType(string name)
    {
        var symbol = GetSymbol(name, SymbolType.CustomType) ?? Symbol.Void;
        return (TypeSymbol)symbol;
    }

    public VariableSymbol GetVariable(string name)
    {
        var symbol = GetSymbol(name, SymbolType.Variable) ?? Symbol.Void;
        return (VariableSymbol)symbol;
    }

    public FunctionSymbol GetFunction(string name)
    {
        var symbol = GetSymbol(name, SymbolType.Function) ?? Symbol.Void;
        return (FunctionSymbol)symbol;
    }

    public bool HasSymbol(string name, SymbolType type)
    {
        var key = new SymbolKey(name, type);
        return _symbols.ContainsKey(key);
    }

    public bool TryGetSymbol(string name, SymbolType type, [NotNullWhen(true)] out Symbol? symbol)
    {
        var key = new SymbolKey(name, type);
        return _symbols.TryGetValue(key, out symbol);
    }

    public bool TryGetType(string name, [NotNullWhen(true)] out TypeSymbol? type)
    {
        var key = new SymbolKey(name, SymbolType.CustomType);
        _symbols.TryGetValue(key, out Symbol? symbol);
        type = symbol as TypeSymbol;

        return symbol != null;
    }

    public bool TryGetVariable(string name, [NotNullWhen(true)] out VariableSymbol? variable)
    {
        var key = new SymbolKey(name, SymbolType.Variable);
        _symbols.TryGetValue(key, out Symbol? symbol);
        variable = symbol as VariableSymbol;

        return symbol != null;
    }

    public bool TryGetFunction(string name, [NotNullWhen(true)] out FunctionSymbol? type)
    {
        var key = new SymbolKey(name, SymbolType.Function);
        _symbols.TryGetValue(key, out Symbol? symbol);
        type = symbol as FunctionSymbol;

        return symbol != null;
    }
}