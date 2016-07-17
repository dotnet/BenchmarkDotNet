using BenchmarkDotNet.Toolchains.Classic;

namespace BenchmarkDotNet.Toolchains
{
    public class RoslynToolchain : Toolchain
    {
        internal RoslynToolchain() : base("Classic", new RoslynGenerator(), new RoslynBuilder(), new Executor())
        {
        }
    }
}