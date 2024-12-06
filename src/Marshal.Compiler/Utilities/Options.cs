using CommandLine;

namespace Marshal.Compiler.Utilities;

public sealed class Options
{
    [Option('i', "input", Required = true, HelpText = "Provide all the inputs file")]
    public IEnumerable<string> Inputs { get; set; } = null!;

    [Option('o', "output", Required = false, HelpText = "Set the output program file")]
    public string Output { get; set; } = "program";
}