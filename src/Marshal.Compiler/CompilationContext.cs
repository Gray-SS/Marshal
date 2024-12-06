namespace Marshal.Compiler;

public class CompilationContext 
{
    public SourceFile Source { get; }

    public CompilationContext(SourceFile sourceFile)
    {
        Source = sourceFile;
    }
}