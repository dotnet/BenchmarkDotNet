using BenchmarkDotNet.Toolchains.Classic;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains
{
    /// <summary>
    /// Build a benchmark program with the Roslyn compiler.
    /// </summary>
    public class RoslynToolchain : Toolchain
    {
        [PublicAPI("Used in auto-generated .exe when this toolchain is set explicitly in Job definition")]
        public RoslynToolchain() : base("Classic", new RoslynGenerator(), new RoslynBuilder(), new Executor())
        {
        }
    }
}