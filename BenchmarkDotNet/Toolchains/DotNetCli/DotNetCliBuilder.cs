using System;
using System.IO;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    public class DotNetCliBuilder : IBuilder
    {
        internal const string RestoreCommand = "restore --fallbacksource https://dotnet.myget.org/F/dotnet-core/api/v3/index.json";

        private const string Configuration = "RELEASE";

        private const string OutputDirectory = "binaries";

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

        private string Framework { get; }

        public DotNetCliBuilder(string framework)
        {
            Framework = framework;
        }

        /// <summary>
        /// generates project.lock.json that tells compiler where to take dlls and source from
        /// and builds executable and copies all required dll's
        /// </summary>
        public BuildResult Build(GenerateResult generateResult, ILogger logger, Benchmark benchmark)
        {
            if (!DotNetCliCommandExecutor.ExecuteCommand(
                RestoreCommand, 
                generateResult.DirectoryPath, 
                logger, 
                DefaultTimeout))
            {
                return new BuildResult(generateResult, false, new Exception("dotnet restore has failed"), null);
            }

            if (!DotNetCliCommandExecutor.ExecuteCommand(
                GetBuildCommand(Framework), 
                generateResult.DirectoryPath, 
                logger,
                DefaultTimeout))
            {
                // dotnet cli could have succesfully builded the program, but returned 1 as exit code because it had some warnings
                // so we need to check whether the exe exists or not, if it does then it is OK
                var executablePath = BuildExecutablePath(generateResult, benchmark);
                if (File.Exists(executablePath))
                {
                    return new BuildResult(generateResult, true, null, executablePath);
                }

                return new BuildResult(generateResult, false, new Exception("dotnet build has failed"), null);
            }

            return new BuildResult(generateResult, true, null, BuildExecutablePath(generateResult, benchmark));
        }

        internal static string GetBuildCommand(string frameworkMoniker)
        {
            return $"build --framework {frameworkMoniker} --configuration {Configuration} --output {OutputDirectory}";
        }

        /// <summary>
        /// we use custom output path in order to avoid any future problems related to dotnet cli paths changing
        /// </summary>
        private string BuildExecutablePath(GenerateResult generateResult, Benchmark benchmark)
            => Path.Combine(generateResult.DirectoryPath, OutputDirectory, $"{benchmark.ShortInfo}{RuntimeInformation.ExecutableExtension}");
    }
}