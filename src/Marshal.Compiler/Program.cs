using Marshal.Compiler;
using Marshal.Core.Errors;
using Marshal.Core.Utils;

var errorHandler = new ErrorHandler(); 
if (!ArgumentParser.Parse(args, errorHandler, out Options options))
    return;

var compiler = new Compiler(options);
compiler.Compile();