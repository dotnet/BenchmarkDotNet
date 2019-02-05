using System.Collections.Immutable;
using System.IO;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.Parameters;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public sealed class DotNetCliExecutor : OutOfProcessExecutor
    {
        private string CustomDotNetCliPath { get; }

        public DotNetCliExecutor(string customDotNetCliPath) => CustomDotNetCliPath = customDotNetCliPath;

        protected override (string fileName, string arguments) GetProcessStartArguments(ExecuteParameters parameters)
            =>
            (
                CustomDotNetCliPath ?? "dotnet",
                $"{Path.GetFileName(parameters.BuildResult.ArtifactsPaths.ExecutablePath).Escape()} {parameters.BenchmarkId.ToArguments()}"
            );

        protected override ImmutableArray<EnvironmentVariable> GetImplicitEnvironmentVariables(ExecuteParameters executeParameters)
            => string.IsNullOrEmpty(CustomDotNetCliPath)
                ? ImmutableArray<EnvironmentVariable>.Empty
                : ImmutableArray.Create(new EnvironmentVariable(DotNetCliCommandExecutor.DotnetMultiLevelLookupEnvVarName, "0"));

    }
}