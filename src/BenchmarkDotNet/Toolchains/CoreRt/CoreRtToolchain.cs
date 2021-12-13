using System;
using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.CoreRt
{
    public class CoreRtToolchain : Toolchain
    {
        /// <summary>
        /// compiled as netcoreapp2.0, targets latest (6.0.0-*) CoreRT build from the new feed: https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json
        /// </summary>
        public static readonly IToolchain Core20 = CreateBuilder().UseCoreRtNuGet().TargetFrameworkMoniker("netcoreapp2.0").ToToolchain();
        /// <summary>
        /// compiled as netcoreapp2.1, targets latest (6.0.0-*) CoreRT build from the new feed: https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json
        /// </summary>
        public static readonly IToolchain Core21 = CreateBuilder().UseCoreRtNuGet().TargetFrameworkMoniker("netcoreapp2.1").ToToolchain();
        /// <summary>
        /// compiled as netcoreapp2.2, targets latest (6.0.0-*) CoreRT build from the new feed: https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json
        /// </summary>
        public static readonly IToolchain Core22 = CreateBuilder().UseCoreRtNuGet().TargetFrameworkMoniker("netcoreapp2.2").ToToolchain();
        /// <summary>
        /// compiled as netcoreapp3.0, targets latest (6.0.0-*) CoreRT build from the new feed: https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json
        /// </summary>
        public static readonly IToolchain Core30 = CreateBuilder().UseCoreRtNuGet().TargetFrameworkMoniker("netcoreapp3.0").ToToolchain();
        /// <summary>
        /// compiled as netcoreapp3.1, targets latest (6.0.0-*) CoreRT build from the new feed: https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json
        /// </summary>
        public static readonly IToolchain Core31 = CreateBuilder().UseCoreRtNuGet().TargetFrameworkMoniker("netcoreapp3.1").ToToolchain();
        /// <summary>
        /// compiled as net5.0, targets latest (6.0.0-*) CoreRT build from the new feed: https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json
        /// </summary>
        public static readonly IToolchain Core50 = CreateBuilder().UseCoreRtNuGet().TargetFrameworkMoniker("net5.0").ToToolchain();
        /// <summary>
        /// compiled as net6.0, targets latest (6.0.0-*) CoreRT build from the new feed: https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json
        /// </summary>
        public static readonly IToolchain Core60 = CreateBuilder().UseCoreRtNuGet().TargetFrameworkMoniker("net6.0").ToToolchain();
        /// </summary>
        public static readonly IToolchain Core70 = CreateBuilder().UseCoreRtNuGet().TargetFrameworkMoniker("net7.0").ToToolchain();

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
