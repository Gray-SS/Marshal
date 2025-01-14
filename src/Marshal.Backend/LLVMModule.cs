using Swigged.LLVM;

namespace Marshal.Backend;

public class LLVMModule : BackendModule
{
    public ModuleRef ModuleRef { get; }
    public BuilderRef BuilderRef { get; }

    public LLVMModule(string name, ModuleRef moduleRef, BuilderRef builderRef) : base(name)
    {
        ModuleRef = moduleRef;
        BuilderRef = builderRef;
    }

    public override void WriteToFile(string path)
    {
        LLVM.WriteBitcodeToFile(ModuleRef, path);
    }
}