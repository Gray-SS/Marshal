using Marshal.Compiler.Semantics;
using Swigged.LLVM;

namespace Marshal.Compiler.IR;

public class LLVMTypeResolver
{
    private readonly Dictionary<MarshalType, TypeRef> _llvmTypesMap = new()
    {
        { MarshalType.Boolean, LLVM.Int8Type() }, 
        { MarshalType.Byte, LLVM.Int8Type() }, 
        { MarshalType.Char, LLVM.Int8Type() }, 
        { MarshalType.Short, LLVM.Int16Type() }, 
        { MarshalType.Int, LLVM.Int32Type() }, 
        { MarshalType.Long, LLVM.Int64Type() },
        { MarshalType.Void, LLVM.VoidType() },
    };

    public TypeRef Resolve(MarshalType type)
    {
        switch (type)
        {
            case PrimitiveType:
                return _llvmTypesMap[type];
            case PointerType pointer:
                return LLVM.PointerType(Resolve(pointer.Pointee), 0);
            case ArrayType array:
                return LLVM.PointerType(Resolve(array.ElementType), 0);
            case TypeAlias alias:
                return Resolve(alias.Aliased);
            default:
                throw new NotImplementedException();
        }
    }
}