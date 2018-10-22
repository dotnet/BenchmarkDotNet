using System;
using BenchmarkDotNet.Jobs;
using System.Linq;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public class DotNetCliBuilder : IBuilder
    {
        private string TargetFrameworkMoniker { get; }

        private string CustomDotNetCliPath { get; }

        [PublicAPI]
        public DotNetCliBuilder(string targetFrameworkMoniker, string customDotNetCliPath = null)
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            CustomDotNetCliPath = customDotNetCliPath;
        }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
            => new DotNetCliCommand(
            //TODO: This doesn't actually use the generate script in the DotNetCliGenerator to do the building... so instead we'll do the nuget checks here as well for now
            // see: https://github.com/dotnet/BenchmarkDotNet/issues/804
            var nugetCommands = DotNetCliGenerator.GetNugetPackageCliCommands(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver);
            foreach(var command in nugetCommands)
            {
                var addPackageResult = DotNetCliCommandExecutor.ExecuteCommand(
                    CustomDotNetCliPath,
                    command,
                    generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath,
                    logger,
                    useSharedCompilation: null); //exclude the UseSharedCompilation command since that's not compatible with a dotnet add package

                if (!addPackageResult.IsSuccess)
                    return BuildResult.Failure(generateResult, new Exception(addPackageResult.ProblemDescription));
            }

                    CustomDotNetCliPath, 
                    string.Empty, 
                    generateResult, 
                    logger, 
                    buildPartition,
                    Array.Empty<EnvironmentVariable>())
                .RestoreThenBuild();
    }
}
