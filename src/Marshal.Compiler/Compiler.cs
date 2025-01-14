using System.Diagnostics;
using Marshal.Backend;
using Marshal.Backend.Emit;
using Marshal.Backend.IR;
using Marshal.Backend.Utils;
using Marshal.Core;
using Marshal.Core.Errors;
using Marshal.Core.Semantics;
using Marshal.Core.Syntax;
using Marshal.Core.Utils;

namespace Marshal.Compiler;

public class Compiler
{
    public SymbolTable GlobalTable { get; }

    private readonly Options _options;
    private readonly ErrorHandler _errorHandler = new(ErrorLogger.Default);

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

        var paths = _options.InputPaths;
        if (paths.Count == 0)
        {
            success = false;
            _errorHandler.Report(ErrorType.Fatal, "aucun fichier source fourni");
        }

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
            CommandExecutor.ExecuteCommand($"clang {string.Join(' ', objs)} -o {_options.OutputPath}");

            foreach (var obj in objs)
                File.Delete(obj);
        }

        sw.Stop();
        
        var color = success ? ConsoleColor.DarkGreen : ConsoleColor.Yellow;
        ConsoleHelper.WriteLine(color, $"compilation terminée {(success ? "avec succès" : "avec échec")}.");
        ConsoleHelper.WriteLine(ConsoleColor.DarkGray, $"temps écoulé: {sw.Elapsed}");

        return success;
    }

    private bool CompileFile(string relativePath, out string objectPath)
    {
        objectPath = string.Empty;

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

        var context = new CompilationContext(relativePath);

        var lexer = new Lexer(context, _errorHandler);
        List<Token> tokens = lexer.Tokenize();
        
        var parser = new Parser(tokens, context, _errorHandler);
        CompilationUnit unit = parser.ParseAST();

        var symbolTableBuilder = new SymbolTableBuilder(unit, context, _errorHandler);
        symbolTableBuilder.Process();

        var semanticAnalyzer = new SemanticAnalyzer(unit, context, _errorHandler);
        semanticAnalyzer.Process();

        var irGenerator = new IRGenerator(unit, context, _errorHandler);
        IBackendModule module = irGenerator.Generate();

        var emitter = new ObjectEmitter(module, context, _errorHandler);
        objectPath = emitter.Emit();

        return true;
    }
}