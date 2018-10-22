using System;
using System.IO;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli 
{
    public struct DotNetCliCommandResult
    {
        [PublicAPI] public bool IsSuccess { get; }

        [PublicAPI] public TimeSpan ExecutionTime { get; }

        [PublicAPI] public string StandardOutput { get; }

        [PublicAPI] public string StandardError { get; }

        /// <summary>
        /// in theory, all errors should be reported to standard error, 
        /// but sometimes they are not so we can at least return 
        /// standard output which hopefully will contain some useful information
        /// </summary>
        public string ProblemDescription => HasNonEmptyErrorMessage ? StandardError : StandardOutput;

        [PublicAPI] public bool HasNonEmptyErrorMessage => !string.IsNullOrEmpty(StandardError);

        private DotNetCliCommandResult(bool isSuccess, TimeSpan executionTime, string standardOutput, string standardError)
        {
            IsSuccess = isSuccess;
            ExecutionTime = executionTime;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        public static DotNetCliCommandResult Success(TimeSpan time, string standardOutput)
            => new DotNetCliCommandResult(true, time, standardOutput, string.Empty);

        public static DotNetCliCommandResult Failure(TimeSpan time, string standardError, string standardOutput)
            => new DotNetCliCommandResult(false, time, standardOutput, standardError);

        [PublicAPI]
        public BuildResult ToBuildResult(GenerateResult generateResult)
            => IsSuccess || File.Exists(generateResult.ArtifactsPaths.ExecutablePath) // dotnet cli could have successfully built the program, but returned 1 as exit code because it had some warnings
                ? BuildResult.Success(generateResult)
                : BuildResult.Failure(generateResult, new Exception(ProblemDescription));
    }
}