namespace BenchmarkDotNet.Toolchains.Classic
{
#if !UAP
    public class ClassicToolchain
    {
        public static readonly IToolchain Instance
#if CLASSIC
            = new RoslynToolchain();
#else
            = new Net46Toolchain();
#endif
    }
#endif
}