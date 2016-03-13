using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Toolchains.Classic;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.Dnx
{
    public class DnxToolchain : Toolchain
    {
        private const string TargetFrameworkMoniker = "dnx451";

        public static readonly IToolchain Instance = new DnxToolchain();

        private DnxToolchain() 
            : base("Dnx",
                  new DotNetCliGenerator(
                      TargetFrameworkMoniker,
                      extraDependencies: "\"frameworkAssemblies\": { \"System.Runtime\": \"4.0.10.0\" }",
                      platformProvider: platform => platform.ToConfig()),
                  new DotNetCliBuilder(TargetFrameworkMoniker), 
                  new ClassicExecutor())
        {
        }
    }
}