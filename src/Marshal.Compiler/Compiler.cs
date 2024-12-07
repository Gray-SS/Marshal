using Swigged.LLVM;
using System.Diagnostics;
using Marshal.Compiler.CodeGen;
using Marshal.Compiler.Errors;
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
        GlobalTable.AddSymbol(Symbol.Byte);
        GlobalTable.AddSymbol(Symbol.Short);
        GlobalTable.AddSymbol(Symbol.String);
        GlobalTable.AddSymbol(Symbol.Int);
        GlobalTable.AddSymbol(Symbol.Long);
        GlobalTable.AddSymbol(Symbol.Char);
        GlobalTable.AddSymbol(Symbol.Void);
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
            _errorHandler.Report(ErrorType.Fatal, $"un fichier source a été vide");
            return false;   
        }

        if (!File.Exists(relativePath))
        {
            _errorHandler.Report(ErrorType.Fatal, $"le fichier source '{Path.GetFullPath(relativePath)}' n'a pas pu être trouvé");
            return false;
        }

        var sourceFile = new SourceFile(relativePath);
        var ctx = new CompilationContext(sourceFile);

        var lexer = new Lexer(ctx, _errorHandler);
        var tokens = lexer.Tokenize();

        // Console.WriteLine(string.Join('\n', tokens.Select(x => $"[{x.Type}:{x.Value}]")));

        if (_errorHandler.HasError) return false;

        var parser = new Parser(tokens, ctx, _errorHandler);
        var ast = parser.ParseProgram();

        if (_errorHandler.HasError) return false;

        var semanticAnalyzer = new SemanticAnalyzer(GlobalTable, _errorHandler);
        semanticAnalyzer.Visit(ast);

        if (_errorHandler.HasError) return false;

        var module = LLVM.ModuleCreateWithName(ctx.Source.FullPath);

        var codeGen = new CodeGenerator(module);
        codeGen.Visit(ast);

        if (_errorHandler.HasError) return false;

        string llvmFile = $"{Path.Combine(Path.GetDirectoryName(ctx.Source.RelativePath)!, Path.GetFileNameWithoutExtension(ctx.Source.RelativePath))}.mo";

        var errorMsg = new MyString();
        LLVM.DumpModule(module);

        LLVM.VerifyModule(module, VerifierFailureAction.AbortProcessAction, errorMsg);
        LLVM.WriteBitcodeToFile(module, llvmFile);

        string fileObj = $"{Path.Combine(Path.GetDirectoryName(ctx.Source.RelativePath)!, Path.GetFileNameWithoutExtension(ctx.Source.RelativePath))}.o";

        if (!CommandExecutor.ExecuteCommand($"llc {llvmFile} -filetype=obj -o {fileObj}"))
            return false;
        
        objFile = fileObj;
        File.Delete(llvmFile);

        return true;
    }
}