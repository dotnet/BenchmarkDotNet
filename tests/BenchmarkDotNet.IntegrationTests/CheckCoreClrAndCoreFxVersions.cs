using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.IntegrationTests
{
    public abstract class CheckCoreClrAndCoreFxVersions
    {
        private string ExpectedCoreClrVersion { get; }

        private string ExpectedCoreFxVersion { get; }

        protected CheckCoreClrAndCoreFxVersions(string expectedCoreClrVersion = null, string expectedCoreFxVersion = null)
        {
            ExpectedCoreClrVersion = expectedCoreClrVersion;
            ExpectedCoreFxVersion = expectedCoreFxVersion;
        }

        [Benchmark]
        public void Check()
        {
            var coreFxAssemblyInfo = FileVersionInfo.GetVersionInfo(typeof(Regex).GetTypeInfo().Assembly.Location);
            var coreClrAssemblyInfo = FileVersionInfo.GetVersionInfo(typeof(object).GetTypeInfo().Assembly.Location);

            Console.WriteLine($"// CoreFx version: {coreFxAssemblyInfo.FileVersion}, location {typeof(Regex).GetTypeInfo().Assembly.Location}, product version {coreFxAssemblyInfo.ProductVersion}");
            Console.WriteLine($"// CoreClr version {coreClrAssemblyInfo.FileVersion}, location {typeof(object).GetTypeInfo().Assembly.Location}, product version {coreClrAssemblyInfo.ProductVersion}");

            if (ExpectedCoreFxVersion != null && coreFxAssemblyInfo.FileVersion != ExpectedCoreFxVersion)
                throw new Exception($"Wrong CoreFx version: was {coreFxAssemblyInfo.FileVersion}, should be {ExpectedCoreFxVersion}");

            if (ExpectedCoreClrVersion != null && coreClrAssemblyInfo.FileVersion != ExpectedCoreClrVersion)
                throw new Exception($"Wrong CoreClr version: was {coreClrAssemblyInfo.FileVersion}, should be {ExpectedCoreClrVersion}");
        }
    }
}