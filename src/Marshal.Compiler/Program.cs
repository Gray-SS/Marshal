using CommandLine;
using Marshal.Compiler;
using Marshal.Compiler.Utilities;

Parser.Default.ParseArguments<Options>(args)
    .WithParsed(o => {
        var compiler = new Compiler(o);
        if (!compiler.Compile())
            Environment.Exit(1);
    });
