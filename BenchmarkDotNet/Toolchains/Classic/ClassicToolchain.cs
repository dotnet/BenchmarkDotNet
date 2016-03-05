#if CLASSIC
namespace BenchmarkDotNet.Toolchains.Classic
{
    public class ClassicToolchain : Toolchain
    {
        public static readonly IToolchain Instance = new ClassicToolchain();

        private ClassicToolchain() : base("Classic", new ClassicGenerator(), new ClassicBuilder(), new ClassicExecutor())
        {
        }
    }
}
#endif