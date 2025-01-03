using Marshal.Compiler.Semantics;
using Swigged.LLVM;

namespace Marshal.Compiler.IR;

public class LLVMTypeResolver
{
    private readonly ContextRef _ctx;
    private readonly Dictionary<string, Struct> _structs;
    private readonly Dictionary<MarshalType, TypeRef> _llvmTypesMap;

    public LLVMTypeResolver(ContextRef ctx, Dictionary<string, Struct> structs)
    {
        _ctx = ctx;
        _structs = structs;
        _llvmTypesMap = new()
        {
            { MarshalType.Boolean, LLVM.Int1TypeInContext(ctx) }, 
            { MarshalType.Byte, LLVM.Int8TypeInContext(ctx) }, 
            { MarshalType.Char, LLVM.Int8TypeInContext(ctx) }, 
            { MarshalType.Short, LLVM.Int16TypeInContext(ctx) }, 
            { MarshalType.Int, LLVM.Int32TypeInContext(ctx) }, 
            { MarshalType.Long, LLVM.Int64TypeInContext(ctx) },
            { MarshalType.Void, LLVM.VoidTypeInContext(ctx) },
        };

    }

    public TypeRef Resolve(MarshalType type)
    {
        return type switch
        {
            PrimitiveType => _llvmTypesMap[type],
            PointerType pointer => LLVM.PointerType(Resolve(pointer.Pointee), 0),
            ArrayType array => LLVM.PointerType(Resolve(array.ElementType), 0),
            StringType => LLVM.PointerType(Resolve(MarshalType.Char), 0),
            TypeAlias alias => Resolve(alias.Aliased),
            StructType @struct => _structs[@struct.Name].Type,
            _ => throw new NotImplementedException(),
        };
    }
}