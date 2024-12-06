namespace Marshal.Compiler.Syntax;

public readonly struct Location
{
    public int Column { get; }
    public int Line { get; }
    public string RelativePath { get; }

    public Location(int col, int line, string relativePath)
    {
        Column = col;
        Line = line;
        RelativePath = relativePath;
    }
}