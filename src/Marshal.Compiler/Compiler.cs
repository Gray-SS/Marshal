using System.Diagnostics;
using Marshal.Compiler.Emit;
using Marshal.Compiler.Errors;
using Marshal.Compiler.IR;
using Marshal.Compiler.Semantics;
using Marshal.Compiler.Syntax;
using Marshal.Compiler.Utilities;

namespace Marshal.Compiler;

public class Compiler
{
    public SymbolTable GlobalTable { get; }

    private readonly Options _options;
    private readonly ErrorHandler _errorHandler = new();

    public Compiler(Options options)
    {
        _options = options;

        GlobalTable = new SymbolTable();
        GlobalTable.AddSymbol(MarshalType.Byte);
        GlobalTable.AddSymbol(MarshalType.Boolean);
        GlobalTable.AddSymbol(MarshalType.Short);
        GlobalTable.AddSymbol(MarshalType.Int);
        GlobalTable.AddSymbol(MarshalType.Long);
        GlobalTable.AddSymbol(MarshalType.Char);
        GlobalTable.AddSymbol(MarshalType.Void);
        GlobalTable.AddSymbol(MarshalType.String);
    }

    public bool Compile()
    {
        Stopwatch sw = Stopwatch.StartNew();

        bool success = true;

        var paths = _options.Inputs;
        if (paths.Any())
        {
            var objs = new List<string>(); 

            foreach (string path in paths)
            {
                if (!CompileFile(path, out string objFile))
                {
                    success = false;
                    continue;
                }

                objs.Add(objFile);
            }

            if (success)
            {
                CommandExecutor.ExecuteCommand($"clang {string.Join(' ', objs)} -o {_options.Output}");

                foreach (var obj in objs)
                    File.Delete(obj);
            }

            sw.Stop();
        }
        else
        {
            success = false;
            _errorHandler.Report(ErrorType.Fatal, "aucun fichier source fourni");
        }
        
        var color = success ? ConsoleColor.DarkGreen : ConsoleColor.Yellow;
        ConsoleHelper.WriteLine(color, $"compilation terminée {(success ? "avec succès" : "avec échec")}.");
        ConsoleHelper.WriteLine(ConsoleColor.DarkGray, $"temps écoulé: {sw.Elapsed}");

        return success;
    }

    private bool CompileFile(string relativePath, out string objFile)
    {
        objFile = string.Empty;

        if (string.IsNullOrEmpty(relativePath))
        {
            _errorHandler.Report(ErrorType.Fatal, $"le nom des fichiers sources ne peuvent pas être vide");
            return false;   
        }

        if (!File.Exists(relativePath))
        {
            _errorHandler.Report(ErrorType.Fatal, $"le fichier source '{Path.GetFullPath(relativePath)}' n'a pas pu être trouvé");
            return false;
        }

        var context = new CompilationContext(relativePath, GlobalTable);
        var passes = new List<CompilerPass>() 
        {
            new Lexer(context, _errorHandler),
            new Parser(context, _errorHandler),
            new SymbolTableBuilder(context, _errorHandler),
            new SemanticAnalyzer(context, _errorHandler),
            new IRGenerator(context, _errorHandler),
            new ObjectEmitter(context, _errorHandler),
        };

        foreach (CompilerPass pass in passes)
        {
            pass.Apply();

            if (_errorHandler.HasError)
                return false;
        }

        objFile = context.ObjFilePath;
        return true;
    }
}