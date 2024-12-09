using Marshal.Compiler.Errors;
using Marshal.Compiler.Utilities;
using Swigged.LLVM;

namespace Marshal.Compiler.Emit;

public class ObjectEmitter : CompilerPass
{
    public ObjectEmitter(CompilationContext context, ErrorHandler errorHandler) : base(context, errorHandler)
    {
    }

    public override void Apply()
    {
        ModuleRef module = Context.Module;

        string llvmPath = $"{Path.ChangeExtension(Context.RelativePath, ".ll")}";
        LLVM.WriteBitcodeToFile(module, llvmPath);

        string objectPath = $"{Path.ChangeExtension(Context.RelativePath, ".o")}";

        if (!CommandExecutor.ExecuteCommand($"llc -filetype=obj {llvmPath} -o {objectPath}"))
            ErrorHandler.Report(ErrorType.Fatal, "impossible d'exécuter la commande llc. Vérifier que LLVM est correctement installé sur votre système.");

        File.Delete(llvmPath);

        Context.ObjFilePath = objectPath;
    }
}