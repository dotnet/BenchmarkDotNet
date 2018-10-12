using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public static class DotNetCliCommandExecutor
    {
        public class CommandParameters
        {
            [PublicAPI] public string CliPath { get; }
            
            [PublicAPI] public string Arguments { get; }

            [PublicAPI] public ArtifactsPaths ArtifactsPaths { get; }

            [PublicAPI] public ILogger Logger { get; }

            [PublicAPI] public BuildPartition BuildPartition { get; }

            [PublicAPI] public IReadOnlyList<EnvironmentVariable> EnvironmentVariables { get; }
            
            public CommandParameters(string cliPath, string arguments, ArtifactsPaths artifactsPaths, ILogger logger, 
                BuildPartition buildPartition, IReadOnlyList<EnvironmentVariable> environmentVariables)
            {
                CliPath = cliPath;
                Arguments = arguments;
                ArtifactsPaths = artifactsPaths;
                Logger = logger;
                BuildPartition = buildPartition;
                EnvironmentVariables = environmentVariables;
            }
            
            public CommandParameters WithArguments(string arguments)
                => new CommandParameters(CliPath, arguments, ArtifactsPaths, Logger, BuildPartition, EnvironmentVariables);
        }
        
        public struct CommandResult
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

            private CommandResult(bool isSuccess, TimeSpan executionTime, string standardOutput, string standardError)
            {
                IsSuccess = isSuccess;
                ExecutionTime = executionTime;
                StandardOutput = standardOutput;
                StandardError = standardError;
            }

            public static CommandResult Success(TimeSpan time, string standardOutput)
                => new CommandResult(true, time, standardOutput, string.Empty);

            public static CommandResult Failure(TimeSpan time, string standardError, string standardOutput)
                => new CommandResult(false, time, standardOutput, standardError);

            [PublicAPI]
            public BuildResult ToBuildResult(GenerateResult generateResult)
                => IsSuccess || File.Exists(generateResult.ArtifactsPaths.ExecutablePath) // dotnet cli could have successfully built the program, but returned 1 as exit code because it had some warnings
                    ? BuildResult.Success(generateResult)
                    : BuildResult.Failure(generateResult, new Exception(ProblemDescription));
        }

        [PublicAPI]
        public static CommandResult ExecuteCommand(CommandParameters parameters)
        {
            commandWithArguments = $"{commandWithArguments} /p:UseSharedCompilation={useSharedCompilation.ToString().ToLowerInvariant()}";

            using (var process = new Process { StartInfo = BuildStartInfo(parameters.CliPath, parameters.ArtifactsPaths.BuildArtifactsDirectoryPath, parameters.Arguments, parameters.EnvironmentVariables) })
            {
                var stopwatch = Stopwatch.StartNew();
                process.Start();

                string standardOutput = process.StandardOutput.ReadToEnd();
                string standardError = process.StandardError.ReadToEnd();

                process.WaitForExit();
                stopwatch.Stop();

                parameters.Logger.WriteLineInfo($"// {parameters.Arguments} took {stopwatch.Elapsed.TotalSeconds:0.##}s and exited with {process.ExitCode}");

                return process.ExitCode <= 0
                    ? CommandResult.Success(stopwatch.Elapsed, standardOutput)
                    : CommandResult.Failure(stopwatch.Elapsed, standardError, standardOutput);
            }
        }

        public static CommandResult Restore(CommandParameters parameters)
            => ExecuteCommand(parameters.WithArguments(GetRestoreCommand(parameters.ArtifactsPaths, parameters.BuildPartition, parameters.Arguments)));
        
        public static CommandResult Build(CommandParameters parameters)
            => ExecuteCommand(parameters.WithArguments(GetBuildCommand(parameters.BuildPartition, parameters.Arguments)));
        
        public static CommandResult Publish(CommandParameters parameters)
            => ExecuteCommand(parameters.WithArguments(GetPublishCommand(parameters.BuildPartition, parameters.Arguments)));
        
        internal static string GetRestoreCommand(ArtifactsPaths artifactsPaths, BuildPartition buildPartition, string extraArguments = null) 
            => new StringBuilder(100)
                .Append("restore ")
                .Append(string.IsNullOrEmpty(artifactsPaths.PackagesDirectoryName) ? string.Empty : $"--packages \"{artifactsPaths.PackagesDirectoryName}\" ")
                .Append(GetCustomMsBuildArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver))
                .Append(extraArguments)
                .ToString();
        
        internal static string GetBuildCommand(BuildPartition buildPartition, string extraArguments = null) 
            => new StringBuilder(100)
                .Append($"build -c {buildPartition.BuildConfiguration} ") // we don't need to specify TFM, our auto-generated project contains always single one
                .Append(GetCustomMsBuildArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver))
                .Append(extraArguments)
                .ToString();
        
        internal static string GetPublishCommand(BuildPartition buildPartition, string extraArguments = null) 
            => new StringBuilder(100)
                .Append($"publish -c {buildPartition.BuildConfiguration} ") // we don't need to specify TFM, our auto-generated project contains always single one
                .Append(GetCustomMsBuildArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver))
                .Append(extraArguments)
                .ToString();

        internal static string GetCustomMsBuildArguments(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            if (!benchmarkCase.Job.HasValue(InfrastructureMode.ArgumentsCharacteristic))
                return null;

            var msBuildArguments = benchmarkCase.Job.ResolveValue(InfrastructureMode.ArgumentsCharacteristic, resolver).OfType<MsBuildArgument>();

            return string.Join(" ", msBuildArguments.Select(arg => arg.TextRepresentation));
        }

        internal static string GetDotNetSdkVersion()
        {
            using (var process = new Process { StartInfo = BuildStartInfo(customDotNetCliPath: null, workingDirectory: string.Empty, arguments: "--version") })
            {
                try
                {
                    process.Start();
                }
                catch (Win32Exception) // dotnet cli is not installed
                {
                    return null;
                }

                string output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();

                // first line contains something like ".NET Command Line Tools (1.0.0-beta-001603)"
                return Regex.Split(output, Environment.NewLine, RegexOptions.Compiled)
                    .FirstOrDefault(line => !string.IsNullOrEmpty(line));
            }
        }

        internal static ProcessStartInfo BuildStartInfo(string customDotNetCliPath, string workingDirectory, string arguments,
            IReadOnlyList<EnvironmentVariable> environmentVariables = null, bool redirectStandardInput = false)
        {
            const string dotnetMultiLevelLookupEnvVarName = "DOTNET_MULTILEVEL_LOOKUP";

            var startInfo = new ProcessStartInfo
            {
                FileName = customDotNetCliPath ?? "dotnet",
                WorkingDirectory = workingDirectory,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = redirectStandardInput
            };

            if (environmentVariables != null)
                foreach (var environmentVariable in environmentVariables)
                    startInfo.EnvironmentVariables[environmentVariable.Key] = environmentVariable.Value;

            if (!string.IsNullOrEmpty(customDotNetCliPath) && (environmentVariables == null || environmentVariables.All(envVar => envVar.Key != dotnetMultiLevelLookupEnvVarName)))
                startInfo.EnvironmentVariables[dotnetMultiLevelLookupEnvVarName] = "0";

            return startInfo;
        }
    }
}