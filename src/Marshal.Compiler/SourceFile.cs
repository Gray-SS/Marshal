namespace Marshal.Compiler;

public class SourceFile
{
    public string FullPath { get; }
    public string RelativePath { get; }
    public string Content { get; }

    public SourceFile(string relativePath)
    {
        RelativePath = relativePath;
        FullPath = Path.GetFullPath(relativePath);
        Content = File.ReadAllText(relativePath);
    }
}