using Marshal.Compiler.Semantics;
using Swigged.LLVM;

namespace Marshal.Compiler.IR;

// public interface IMemoryAllocator
// {
//     ValueRef AllocVariable(BuilderRef builder, MarshalType type, TypeRef llvmType);

//     ValueRef Malloc(BuilderRef builder, MarshalType type, TypeRef llvmType);
// }

// public sealed class MemoryAllocator : IMemoryAllocator
// {
//     public ValueRef Malloc(BuilderRef builder, MarshalType type, TypeRef llvmType)
//     {
//         if (!type.IsReferenced)
//             throw new InvalidOperationException("Couldn't malloc a value type.");

//         return LLVM.BuildMalloc(builder, llvmType, "malloc_ptr");
//     }
// }