using Swigged.LLVM;

namespace Marshal.Compiler.IR;

public static class LLVMHelper
{
    public static readonly ValueRef ZeroInt = LLVM.ConstInt(LLVM.Int32Type(), 0, true);
    public static readonly ValueRef OneInt = LLVM.ConstInt(LLVM.Int32Type(), 1, true);
    public static readonly ValueRef MinusOneInt = LLVM.ConstInt(LLVM.Int32Type(), 0xFFFFFFFF, true);

    public static void BuildIncrement(BuilderRef builder, ValueRef varPtr)
    {
        ValueRef varValue = LLVM.BuildLoad(builder, varPtr, "var_value");
        ValueRef result = LLVM.BuildAdd(builder, varValue, OneInt, "inc_result");
        LLVM.BuildStore(builder, result, varPtr);
    }

    public static void BuildDecrement(BuilderRef builder, ValueRef varPtr)
    {
        ValueRef varValue = LLVM.BuildLoad(builder, varPtr, "var_value");
        ValueRef result = LLVM.BuildSub(builder, varValue, OneInt, "dec_result");
        LLVM.BuildStore(builder, result, varPtr);
    }

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