using BenchmarkDotNet.Toolchains.Classic;

namespace BenchmarkDotNet.Toolchains
{
    /// <summary>
    /// Build a benchmark program with the Roslyn compiler.
    /// </summary>
    public class RoslynToolchain : Toolchain
    {
        internal RoslynToolchain() : base("Classic", new RoslynGenerator(), new RoslynBuilder(), new Executor())
        {
        }
    }
}