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
    public IEnumerable<Symbol> Symbols => _scopes.SelectMany(x => x.Values);

    private readonly Stack<Dictionary<SymbolKey, Symbol>> _scopes;

    public SymbolTable()
    {
        _scopes = new Stack<Dictionary<SymbolKey, Symbol>>();
        EnterScope();
    }

    public void EnterScope()
    {
        _scopes.Push([]);
    }

    public void ExitScope()
    {
        if (_scopes.Count > 1)
        {
            _scopes.Pop();
        }
        else
            throw new InvalidOperationException("impossible de sortir de la scope actuelle.");
    }

    public bool AddSymbol(Symbol symbol)
    {
        var key = new SymbolKey(symbol.Name, symbol.Type);
        return _scopes.Peek().TryAdd(key, symbol);
    }

    public Symbol? GetSymbol(string name, SymbolType type)
    {
        var key = new SymbolKey(name, type);

        foreach (var scope in _scopes)
        {
            if (scope.TryGetValue(key, out Symbol? symbol))
                return symbol;
        }
        
        return null;
    }

    public TypeSymbol GetType(string name)
    {
        var symbol = GetSymbol(name, SymbolType.Type) ?? Symbol.Void;
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

        foreach (var scope in _scopes)
        {
            if (scope.ContainsKey(key))
                return true;
        }
        
        return false;
    }

    public bool TryGetSymbol(string name, SymbolType type, [NotNullWhen(true)] out Symbol? symbol)
    {
        symbol = GetSymbol(name, type);
        return symbol != null;
    }

    public bool TryGetType(string name, [NotNullWhen(true)] out TypeSymbol? type)
    {
        type = GetSymbol(name, SymbolType.Type) as TypeSymbol;
        return type != null;
    }

    public bool TryGetVariable(string name, [NotNullWhen(true)] out VariableSymbol? variable)
    {
        variable = GetSymbol(name, SymbolType.Variable) as VariableSymbol;
        return variable != null;
    }

    public bool TryGetFunction(string name, [NotNullWhen(true)] out FunctionSymbol? function)
    {
        function = GetSymbol(name, SymbolType.Function) as FunctionSymbol;
        return function != null;
    }
}