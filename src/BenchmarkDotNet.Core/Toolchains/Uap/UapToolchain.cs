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
                  new UapGenerator(),
                  new UapBuilder(),
                  new UapExecutor())
        {
        }

        private static string GetExtraDependencies()
        {
            return "\"dependencies\": { \"Microsoft.NETCore.UniversalWindowsPlatform\": { \"version\": \"5.1.0\" } },";
        }

        private static string GetImports()
        {
            return "[]";
            //return "[ \"dnxcore50\", \"portable-net45+win8\", \"dotnet5.6\", \"netcore50\" ]";
        }

        private static string GetRuntime()
        {
            var currentRuntime = "win10-x64";
            if (!string.IsNullOrEmpty(currentRuntime))
            {
                return $"\"runtimes\": {{ \"{currentRuntime}\": {{ }} }},";
            }

            return string.Empty;
        }
    }
#endif
}
