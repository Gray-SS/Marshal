using Swigged.LLVM;

namespace Marshal.Compiler.IR;

public static class LLVMHelper
{
    public static readonly ValueRef ZeroInt = LLVM.ConstInt(LLVM.Int32Type(), 0, false);

    public static void BuildMemCpy(BuilderRef builder, ValueRef destPtr, ValueRef srcPtr, uint length)
    {
        for (uint i = 0; i < length; i++)
        {
            ValueRef index = LLVM.ConstInt(LLVM.Int32Type(), i, false);
            ValueRef destEPtr = LLVM.BuildGEP(builder, destPtr, [ ZeroInt, index ], $"destEPtr_{i}");
            ValueRef srcEPtr = LLVM.BuildGEP(builder, srcPtr, [ ZeroInt, index ], $"srcEPtr_{i}");

            ValueRef srcElement = LLVM.BuildLoad(builder, srcEPtr, $"srcEValue_{i}");
            LLVM.BuildStore(builder, srcElement, destEPtr);
        }
    }
}