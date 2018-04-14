using System.Collections.Generic;

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
            string customDotNetCliPath,
            Dictionary<string, string> feeds, bool useNuGetClearTag, bool useTempFolderForRestore)
            : base(displayName,
                new Generator(coreRtVersion, useCppCodeGenerator, runtimeFrameworkVersion, targetFrameworkMoniker, runtimeIdentifier, feeds, useNuGetClearTag, useTempFolderForRestore),
                new Publisher(customDotNetCliPath, ilcPath, useCppCodeGenerator, runtimeIdentifier),
                new Executor())
        {
        }

        public static CoreRtToolchainBuilder CreateBuilder() => CoreRtToolchainBuilder.Create();
    }
}