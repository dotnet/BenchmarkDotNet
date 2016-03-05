using BenchmarkDotNet.Toolchains.Classic;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.Core
{
    public class CoreToolchain : Toolchain
    {
        private const string TargetFrameworkMoniker = "dnxcore50"; // todo: when dnx gets replaced in VS with dotnet cli replace this name with fancy dotnet5.4 name

        public static readonly IToolchain Instance = new CoreToolchain();

        private CoreToolchain()
            : base("Core",
                  new DotNetCliGenerator(TargetFrameworkMoniker),
                  new DotNetCliBuilder(TargetFrameworkMoniker),
                  new ClassicExecutor())
        {
        }
    }
}