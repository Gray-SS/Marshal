using System.Diagnostics;
using Marshal.Compiler.CodeGen;
using Marshal.Compiler.Errors;
using Marshal.Compiler.Semantics;
using Marshal.Compiler.Syntax;
using Marshal.Compiler.Utilities;

namespace Marshal.Compiler;

public class Compiler
{
    private readonly ErrorHandler _errorHandler = new();
    private readonly SymbolTable _symbolTable;
    private readonly Options _options;

    public Compiler(Options options)
    {
        _options = options;

        _symbolTable = new SymbolTable();
        _symbolTable.AddSymbol(Symbol.Byte);
        _symbolTable.AddSymbol(Symbol.Short);
        _symbolTable.AddSymbol(Symbol.String);
        _symbolTable.AddSymbol(Symbol.Int);
        _symbolTable.AddSymbol(Symbol.Long);
        _symbolTable.AddSymbol(Symbol.Char);
        _symbolTable.AddSymbol(Symbol.Void);
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

        if (_errorHandler.HasError) return false;

        var parser = new Parser(tokens, ctx, _errorHandler);
        var ast = parser.ParseProgram();

        if (_errorHandler.HasError) return false;

        var semanticAnalyzer = new SemanticAnalyzer(_symbolTable, _errorHandler);
        semanticAnalyzer.Analyze(ast);

        if (_errorHandler.HasError) return false;

        var codeGen = new CodeGenerator(_symbolTable, _errorHandler);
        var asm = codeGen.Generate(ast);

        if (_errorHandler.HasError) return false;

        ConsoleHelper.WriteLine(ConsoleColor.DarkGreen, asm);

        string llvmFile = $"{Path.Combine(Path.GetDirectoryName(ctx.Source.RelativePath)!, Path.GetFileNameWithoutExtension(ctx.Source.RelativePath))}.mo";
        File.WriteAllText(llvmFile, asm);

        string fileObj = $"{Path.Combine(Path.GetDirectoryName(ctx.Source.RelativePath)!, Path.GetFileNameWithoutExtension(ctx.Source.RelativePath))}.o";

        if (!CommandExecutor.ExecuteCommand($"llc {llvmFile} -filetype=obj -o {fileObj}"))
            return false;
        
        objFile = fileObj;
        File.Delete(llvmFile);

        return true;
    }
}