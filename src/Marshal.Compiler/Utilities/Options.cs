namespace Marshal.Core.Utils;

public class Options
{
    public List<string> InputPaths { get; set; }
    public string OutputPath { get; set; }

    public Options()
    {
        InputPaths = new List<string>();
        OutputPath = "a.out";
    }
}