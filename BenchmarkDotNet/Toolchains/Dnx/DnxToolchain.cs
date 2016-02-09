namespace BenchmarkDotNet.Toolchains.Dnx
{
    public class DnxToolchain : Toolchain
    {
        public static readonly IToolchain Instance = new DnxToolchain();

        public DnxToolchain() : base("Dnx451", new DnxGenerator(), new DnuBuilder(), new DnxExecutor())
        {
        }
    }
}