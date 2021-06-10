namespace BenchmarkDotNet.Toolchains.MonoAotLLVM
{
    public enum MonoAotCompilerMode
    {
        mini = 0, // default
        llvm,
        wasm
    }
}
