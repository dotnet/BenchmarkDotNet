using BenchmarkDotNet.Toolchains.ProjectJson;

namespace BenchmarkDotNet.Toolchains.Classic
{
    public class ClassicToolchain
    {
        public static readonly IToolchain Instance
#if CLASSIC
            = new Roslyn.RoslynToolchain();
#else
            = new ProjectJsonNet46Toolchain();
#endif
    }
}