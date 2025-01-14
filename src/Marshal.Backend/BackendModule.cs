namespace Marshal.Backend;

public abstract class BackendModule
{
    public string Name { get; }

    public BackendModule(string name)
    {
        Name = name;
    }

    public abstract void WriteToFile(string path);
}