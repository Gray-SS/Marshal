using Marshal.Compiler.Errors;
using Marshal.Compiler.Semantics;
using Marshal.Compiler.Syntax;
using Swigged.LLVM;

namespace Marshal.Compiler;

public class CompilationContext
{
    public string FullPath { get; }
    public string RelativePath { get; }
    public string Content { get; }

    public List<Token> Tokens { get; set; } = null!;
    public CompilationUnit AST { get; set; } = null!;
    public SymbolTable SymbolTable { get; set; } = null!;
    public ModuleRef Module { get; set; }
    public string ObjFilePath { get; set; } = null!;

    public CompilationContext(string relativePath, SymbolTable globalTable)
    {
        SymbolTable = globalTable;
        RelativePath = relativePath;
        FullPath = Path.GetFullPath(relativePath);
        Content = File.ReadAllText(relativePath);
    }
}