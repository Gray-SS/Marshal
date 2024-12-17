using Marshal.Compiler.Semantics;
using Swigged.LLVM;

namespace Marshal.Compiler.IR;

public class LLVMTypeResolver
{
    private readonly Dictionary<MarshalType, TypeRef> _llvmTypesMap = new()
    {
        { MarshalType.Boolean, LLVM.Int1Type() }, 
        { MarshalType.Byte, LLVM.Int8Type() }, 
        { MarshalType.Char, LLVM.Int8Type() }, 
        { MarshalType.Short, LLVM.Int16Type() }, 
        { MarshalType.Int, LLVM.Int32Type() }, 
        { MarshalType.Long, LLVM.Int64Type() },
        { MarshalType.Void, LLVM.VoidType() },
    };

    public TypeRef Resolve(MarshalType type)
    {
        return type switch
        {
            PrimitiveType => _llvmTypesMap[type],
            PointerType pointer => LLVM.PointerType(Resolve(pointer.Pointee), 0),
            ArrayType array => LLVM.PointerType(Resolve(array.ElementType), 0),
            TypeAlias alias => Resolve(alias.Aliased),
            _ => throw new NotImplementedException(),
        };
    }
}