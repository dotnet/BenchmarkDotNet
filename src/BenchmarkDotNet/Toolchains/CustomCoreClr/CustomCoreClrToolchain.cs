using System;
using System.Collections.Generic;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.CustomCoreClr
{
    public class CustomCoreClrToolchain : Toolchain
    {
        internal CustomCoreClrToolchain(string displayName, string coreClrVersion, string coreFxVersion, string runtimeFrameworkVersion,
            string targetFrameworkMoniker, string runtimeIdentifier, 
            string customDotNetCliPath, 
            Dictionary<string, string> feeds, bool useNuGetClearTag, bool useTempFolderForRestore,
            TimeSpan timeout)
            : base(displayName,
                new Generator(coreClrVersion, coreFxVersion, runtimeFrameworkVersion, targetFrameworkMoniker, runtimeIdentifier, customDotNetCliPath, feeds, useNuGetClearTag, useTempFolderForRestore),
                new DotNetCliPublisher(customDotNetCliPath: customDotNetCliPath, timeout: timeout),
                new Executor())
        {
        }

        public static CustomCoreClrToolchainBuilder CreateBuilder() => CustomCoreClrToolchainBuilder.Create();
    }
}
