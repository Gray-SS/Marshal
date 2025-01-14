using Swigged.LLVM;

namespace Marshal.Backend;

public class LLVMContext : BackendContext<LLVMModule>
{
    public ContextRef ContextRef { get; }

    public LLVMContext()
    {
        ContextRef = LLVM.ContextCreate();
    }

    protected override LLVMModule BuildModule(string name)
    {
        ModuleRef module = LLVM.ModuleCreateWithNameInContext(name, ContextRef);
        BuilderRef builder = LLVM.CreateBuilderInContext(ContextRef);

        return new LLVMModule(name, module, builder);
    }
}