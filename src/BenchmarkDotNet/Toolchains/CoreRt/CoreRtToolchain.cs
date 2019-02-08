using System;
using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.CoreRt
{
    public class CoreRtToolchain : Toolchain
    {
        /// <summary>
        /// target latest (1.0.0-alpha-*) CoreRT build from https://dotnet.myget.org/F/dotnet-core/api/v3/index.json
        /// </summary>
        public static readonly IToolchain LatestMyGetBuild = CreateBuilder().UseCoreRtNuGet().ToToolchain();

        internal CoreRtToolchain(string displayName, 
            string coreRtVersion, string ilcPath, bool useCppCodeGenerator,
            string runtimeFrameworkVersion, string targetFrameworkMoniker, string runtimeIdentifier,
            string customDotNetCliPath, string packagesRestorePath,
            Dictionary<string, string> feeds, bool useNuGetClearTag, bool useTempFolderForRestore,
            TimeSpan timeout,
            bool rootAllApplicationAssemblies, bool ilcGenerateCompleteTypeMetadata, bool ilcGenerateStackTraceData)
            : base(displayName,
                new Generator(coreRtVersion, useCppCodeGenerator, runtimeFrameworkVersion, targetFrameworkMoniker, customDotNetCliPath, 
                    runtimeIdentifier, feeds, useNuGetClearTag, useTempFolderForRestore, packagesRestorePath,
                    rootAllApplicationAssemblies, ilcGenerateCompleteTypeMetadata, ilcGenerateStackTraceData),
                new DotNetCliPublisher(customDotNetCliPath, GetExtraArguments(useCppCodeGenerator, runtimeIdentifier), GetEnvironmentVariables(ilcPath), timeout), 
                new Executor())
        {
            IlcPath = ilcPath;
        }

        public string IlcPath { get; }

        public static CoreRtToolchainBuilder CreateBuilder() => CoreRtToolchainBuilder.Create();
        
        private static string GetExtraArguments(bool useCppCodeGenerator, string runtimeIdentifier)
            => useCppCodeGenerator ? $"-r {runtimeIdentifier} /p:NativeCodeGen=cpp" : $"-r {runtimeIdentifier}";
        
        // https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/Documentation/how-to-build-and-run-ilcompiler-in-console-shell-prompt.md#compiling-source-to-native-code-using-the-ilcompiler-you-built
        // we have to pass IlcPath env var to get it working
        private static IReadOnlyList<EnvironmentVariable> GetEnvironmentVariables(string ilcPath)
            => ilcPath == null ? Array.Empty<EnvironmentVariable>() : new[] { new EnvironmentVariable("IlcPath", ilcPath) };
    }
}