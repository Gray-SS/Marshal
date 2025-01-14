namespace Marshal.Core;

public class CompilationContext
{
    public string FullPath { get; }
    public string RelativePath { get; }
    public string Content { get; }

    public CompilationContext(string relativePath)
    {
        RelativePath = relativePath;
        FullPath = Path.GetFullPath(relativePath);
        Content = File.ReadAllText(relativePath);
    }
}