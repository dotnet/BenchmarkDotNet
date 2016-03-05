using BenchmarkDotNet.Toolchains.Classic;
using BenchmarkDotNet.Toolchains.Dnx;

namespace BenchmarkDotNet.Toolchains.Core
{
    public class CoreToolchain : Toolchain
    {
        public static readonly IToolchain Instance = new CoreToolchain();

        private CoreToolchain() : base("Core", new DnxGenerator(), new DnxBuilder(), new ClassicExecutor())
        {
            // todo: implement the toolchain
        }
    }
}