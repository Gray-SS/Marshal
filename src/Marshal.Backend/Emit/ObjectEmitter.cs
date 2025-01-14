using Marshal.Backend.Utils;
using Marshal.Core;
using Marshal.Core.Errors;

namespace Marshal.Backend.Emit;

public class ObjectEmitter : CompilerPass
{
    private readonly IBackendModule _module;

    public ObjectEmitter(IBackendModule module, CompilationContext context, ErrorHandler errorHandler) : base(context, errorHandler)
    {
        _module = module;
    }

    public string Emit()
    {
        string llvmPath = $"{Path.ChangeExtension(Context.RelativePath, ".ll")}";
        _module.WriteToFile(llvmPath);

        string objectPath = $"{Path.ChangeExtension(Context.RelativePath, ".o")}";

        if (!CommandExecutor.ExecuteCommand($"llc -filetype=obj {llvmPath} -o {objectPath}"))
            ErrorHandler.Report(ErrorType.Fatal, "impossible d'exécuter la commande llc. Vérifier que LLVM est correctement installé sur votre système.");

        File.Delete(llvmPath);

        return objectPath;
    }
}