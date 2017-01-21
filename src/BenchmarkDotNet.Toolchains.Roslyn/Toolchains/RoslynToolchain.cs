using BenchmarkDotNet.Toolchains.Classic;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains
{
    /// <summary>
    /// Build a benchmark program with the Roslyn compiler.
    /// </summary>
    public class RoslynToolchain : Toolchain
    {
        /// <summary>
        /// Creates new instance of RoslynToolchain.
        /// </summary>
        [PublicAPI]
        public RoslynToolchain() : base("Classic", new RoslynGenerator(), new RoslynBuilder(), new Executor())
        {
        }
    }
}