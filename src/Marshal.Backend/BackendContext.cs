namespace Marshal.Backend;

public abstract class BackendContext<TModule> where TModule : BackendModule
{
    public IReadOnlyCollection<TModule> Modules => _modules.Values;

    private readonly Dictionary<string, TModule> _modules;

    public BackendContext()
    {
        _modules = new Dictionary<string, TModule>();
    }
    
    protected abstract TModule BuildModule(string name);

    public TModule CreateModule(string name)
    {
        if (_modules.ContainsKey(name))
        {
            //TODO: Duplicated module name, need to handle this case. I need a better way of handling errors tbh
            return null!;
        }

        TModule module = BuildModule(name);
        _modules.Add(name, module);

        return module;
        
    }

    public TModule? GetModule(string name)
    {
        _modules.TryGetValue(name, out TModule? module);
        return module;
    }
}