using CommandLine;
using LLVMSharp.Interop;
using Marshal.Compiler;
using Marshal.Compiler.Utilities;

LLVMModuleRef module = LLVMModuleRef.CreateWithName("test");
LLVMBuilderRef builder = LLVMBuilderRef.Create(LLVMContextRef.Global);

builder.BuildAlloca(LLVMTypeRef.Int32, "test");
module.Dump();


// Parser.Default.ParseArguments<Options>(args)
//     .WithParsed(o => {
//         var compiler = new Compiler(o);
//         if (!compiler.Compile())
//             Environment.Exit(1);
//     });
