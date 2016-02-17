namespace BenchmarkDotNet.Toolchains.Dnx
{
    public class DnxToolchain : Toolchain
    {
        public static readonly IToolchain Instance = new DnxToolchain();

        private DnxToolchain() : base("Dnx", new DnxGenerator(), new DnxBuilder(), new DnxExecutor())
        {
        }
    }
}