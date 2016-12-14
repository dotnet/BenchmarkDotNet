using BenchmarkDotNet.Toolchains.DotNetCli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains.Uap
{
#if !UAP
    public class UapToolchain : Toolchain
    {
        public static readonly IToolchain Instance = new UapToolchain();

        private const string TargetFrameworkMoniker = "uap10.0";

        private UapToolchain()
            : base("Core",
                  new DotNetCliGenerator(
                      TargetFrameworkMoniker,
                      GetExtraDependencies(),
                      platformProvider: _ => "x64", // dotnet cli supports only x64 compilation now
                      imports: GetImports(),
                      runtime: GetRuntime()),
                  new DotNetCliBuilder(TargetFrameworkMoniker),
                  new Executor())
        {
        }

        private static string GetExtraDependencies()
        {
            return "\"dependencies\": { \"Microsoft.NETCore.UniversalWindowsPlatform\": { \"version\": \"5.1.0\" } },";
        }

        private static string GetImports()
        {
            return string.Empty;
            //return "[ \"dnxcore50\", \"portable-net45+win8\", \"dotnet5.6\", \"netcore50\" ]";
        }

        private static string GetRuntime()
        {
            return string.Empty;
        }
    }
#endif
}
