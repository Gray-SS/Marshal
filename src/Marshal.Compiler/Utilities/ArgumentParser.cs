using System.Diagnostics.CodeAnalysis;
using Marshal.Core.Errors;

namespace Marshal.Core.Utilities;

public static class ArgumentParser 
{
    private readonly struct ParsingContext
    {
        public string[] Args { get; }
        public ErrorHandler ErrorHandler { get; }

        public ParsingContext(string[] args, ErrorHandler errorHandler)
        {
            Args = args;
            ErrorHandler = errorHandler;
        } 
    }

    public static bool Parse(string[] args, ErrorHandler errorHandler, out Options options)
    {
        options = new Options();
        var context = new ParsingContext(args, errorHandler);

        for (int i = 0; i < args.Length; i++) 
        {
            if (args[i].StartsWith('-'))
            {
                switch (args[i])
                {
                    case "-o":
                        if (!ConsumeArgument(context, ref i, "the output flag '-o' is expecting a path argument (e.g. -o a.out).", out string? arg))
                            return false;

                        options.OutputPath = arg;
                        break;

                    default:
                        errorHandler.Report(ErrorType.Fatal, $"the option '{args[i]}' isn't supported.");
                        return false;
                }
            }
            else 
            {
                //If it's not starting by '-' then it's considered as a file path
                options.InputPaths.Add(args[i]);
            }
        }

        return true;
    }

    private static bool ConsumeArgument(ParsingContext context, ref int i, string errorMessage, [NotNullWhen(true)] out string? arg)
    {
        arg = null;

        if (i + 1 >= context.Args.Length)
        {
            context.ErrorHandler.Report(ErrorType.Fatal, errorMessage);
            return false;
        }

        arg = context.Args[i++ + 1];
        return true;
    }
    
}