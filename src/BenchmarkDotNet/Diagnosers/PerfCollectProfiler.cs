using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.NativeAot;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers
{
    public class PerfCollectProfiler : IProfiler
    {
        public static readonly IDiagnoser Default = new PerfCollectProfiler(new PerfCollectProfilerConfig(performExtraBenchmarksRun: false));

        private readonly PerfCollectProfilerConfig config;
        private readonly DateTime creationTime = DateTime.Now;
        private readonly Dictionary<BenchmarkCase, FileInfo> benchmarkToTraceFile = [];
        private readonly HashSet<string> cliPathWithSymbolsInstalled = [];
        private FileInfo perfCollectFile = default!;
        private Process perfCollectProcess = default!;

        [PublicAPI]
        public PerfCollectProfiler(PerfCollectProfilerConfig config) => this.config = config;

        public string ShortName => "perf";

        public IEnumerable<string> Ids => [nameof(PerfCollectProfiler)];

        public IEnumerable<IExporter> Exporters => [];

        public IEnumerable<IAnalyser> Analysers => [];

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => config.RunMode;

        public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
            => ValidateAsyncCore(validationParameters);

        private async IAsyncEnumerable<ValidationError> ValidateAsyncCore(ValidationParameters validationParameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!OsDetector.IsLinux())
            {
                yield return new ValidationError(true, "The PerfCollectProfiler works only on Linux!");
                yield break;
            }

            if (libc.getuid() != 0)
            {
                yield return new ValidationError(true, "You must run as root to use PerfCollectProfiler.");
                yield break;
            }

            if (validationParameters.Benchmarks.Any() && !await TryInstallPerfCollect(validationParameters, cancellationToken))
            {
                yield return new ValidationError(true, "Failed to install perfcollect script. Please follow the instructions from https://github.com/dotnet/runtime/blob/main/docs/project/linux-performance-tracing.md");
            }
        }

        public void DisplayResults(ILogger logger)
        {
            if (!benchmarkToTraceFile.Any())
                return;

            logger.WriteLineInfo($"Exported {benchmarkToTraceFile.Count} trace file(s). Example:");
            logger.WriteLineInfo(benchmarkToTraceFile.Values.First().FullName);
        }

        public async ValueTask HandleAsync(HostSignal signal, DiagnoserActionParameters parameters, CancellationToken cancellationToken)
        {
            if (signal == HostSignal.BeforeProcessStart)
                await StartCollection(parameters, cancellationToken).ConfigureAwait(false);
            else if (signal == HostSignal.AfterProcessExit)
                StopCollection(parameters);
        }

        private async ValueTask<bool> TryInstallPerfCollect(ValidationParameters validationParameters, CancellationToken cancellationToken)
        {
            var scriptInstallationDirectory = new DirectoryInfo(validationParameters.Config.ArtifactsPath).CreateIfNotExists();

            perfCollectFile = new FileInfo(Path.Combine(scriptInstallationDirectory.FullName, "perfcollect"));
            if (perfCollectFile.Exists)
            {
                return true;
            }

            var logger = validationParameters.Config.GetCompositeLogger();

            string script = await ResourceHelper.LoadTemplateAsync(perfCollectFile.Name, cancellationToken);
            File.WriteAllText(perfCollectFile.FullName, script);

            if (libc.chmod(perfCollectFile.FullName, libc.FilePermissions.S_IXUSR) != 0)
            {
                int lastError = Marshal.GetLastWin32Error();
                logger.WriteError($"Unable to make perfcollect script an executable, the last error was: {lastError}");
            }
            else
            {
                (int exitCode, var output) = await ProcessHelper.RunAndReadOutputLineByLineAsync(
                    perfCollectFile.FullName,
                    "install -force",
                    perfCollectFile.Directory!.FullName,
                    null,
                    includeErrors: true,
                    logger,
                    cancellationToken
                ).ConfigureAwait(false);

                if (exitCode == 0)
                {
                    logger.WriteLine("Successfully installed perfcollect");
                    return true;
                }

                logger.WriteLineError("Failed to install perfcollect");
                foreach (var outputLine in output)
                {
                    logger.WriteLine(outputLine);
                }
            }

            if (perfCollectFile.Exists)
            {
                perfCollectFile.Delete(); // if the file exists it means that perfcollect is installed
            }

            return false;
        }

        private async ValueTask StartCollection(DiagnoserActionParameters parameters, CancellationToken cancellationToken)
        {
            await EnsureSymbolsForNativeRuntime(parameters, cancellationToken).ConfigureAwait(false);

            var traceName = GetTraceFile(parameters, extension: "").Name;

            var start = new ProcessStartInfo
            {
                FileName = perfCollectFile.FullName,
                Arguments = $"collect \"{traceName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = perfCollectFile.Directory!.FullName
            };

            perfCollectProcess = Process.Start(start)!;
        }

        private void StopCollection(DiagnoserActionParameters parameters)
        {
            var logger = parameters.Config.GetCompositeLogger();

            try
            {
                if (!perfCollectProcess.HasExited)
                {
                    if (libc.kill(perfCollectProcess.Id, libc.Signals.SIGINT) != 0)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        logger.WriteLineError($"kill(perfcollect, SIGINT) failed with {lastError}");
                    }

                    if (!perfCollectProcess.WaitForExit((int)config.Timeout.TotalMilliseconds))
                    {
                        logger.WriteLineError($"The perfcollect script did not stop in {config.Timeout.TotalSeconds}s. It's going to be force killed now.");
                        logger.WriteLineInfo("You can create PerfCollectProfiler providing PerfCollectProfilerConfig with custom timeout value.");

                        perfCollectProcess.KillTree(); // kill the entire process tree
                    }

                    FileInfo traceFile = GetTraceFile(parameters, "trace.zip");
                    if (traceFile.Exists)
                    {
                        benchmarkToTraceFile[parameters.BenchmarkCase] = traceFile;
                    }
                }
                else
                {
                    logger.WriteLineError("For some reason the perfcollect script has finished sooner than expected.");
                    logger.WriteLineInfo($"Please run '{perfCollectFile.FullName} install' as root and re-try.");
                }
            }
            finally
            {
                perfCollectProcess.Dispose();
            }
        }

        private async ValueTask EnsureSymbolsForNativeRuntime(DiagnoserActionParameters parameters, CancellationToken cancellationToken)
        {
            if (parameters.BenchmarkCase.GetToolchain() is CoreRunToolchain)
            {
                return; // it's not needed for a local build of dotnet runtime
            }

            string cliPath = parameters.BenchmarkCase.GetToolchain() switch
            {
                CsProjCoreToolchain core => core.CustomDotNetCliPath,
                NativeAotToolchain nativeAot => nativeAot.CustomDotNetCliPath,
                _ => DotNetCliCommandExecutor.DefaultDotNetCliPath.Value
            };

            if (!cliPathWithSymbolsInstalled.Add(cliPath))
            {
                return;
            }

            string sdkPath = await DotNetCliCommandExecutor.GetSdkPathAsync(cliPath, cancellationToken).ConfigureAwait(false); // /usr/share/dotnet/sdk/
            string dotnetPath = Path.GetDirectoryName(sdkPath)!; // /usr/share/dotnet/
            string[] missingSymbols = Directory.GetFiles(dotnetPath, "lib*.so", SearchOption.AllDirectories)
                .Where(nativeLibPath => !nativeLibPath.Contains("FallbackFolder") && !File.Exists(Path.ChangeExtension(nativeLibPath, "so.dbg")))
                .Select(x => Path.GetDirectoryName(x)!)
                .Distinct()
                .ToArray();

            if (!missingSymbols.Any())
            {
                return; // the symbol files are already where we need them!
            }

            ILogger logger = parameters.Config.GetCompositeLogger();
            // We install the tool in a dedicated directory in order to always use latest version and avoid issues with broken existing configs.
            string toolPath = Path.Combine(Path.GetTempPath(), "BenchmarkDotNet", "symbols");
            DotNetCliCommand cliCommand = new(
                cliPath: cliPath,
                filePath: string.Empty,
                tfm: string.Empty,
                arguments: $"tool install dotnet-symbol --tool-path \"{toolPath}\"",
                generateResult: GenerateResult.Success(ArtifactsPaths.Empty, []),
                logger: logger,
                buildPartition: BuildPartition.Empty,
                environmentVariables: [],
                timeout: TimeSpan.FromMinutes(3),
                logOutput: true); // the following commands might take a while and fail, let's log them

            var installResult = await DotNetCliCommandExecutor.ExecuteAsync(cliCommand, cancellationToken).ConfigureAwait(false);
            if (!installResult.IsSuccess)
            {
                logger.WriteError("Unable to install dotnet symbol.");
                return;
            }

            await DotNetCliCommandExecutor.ExecuteAsync(cliCommand
                .WithCliPath(Path.Combine(toolPath, "dotnet-symbol"))
                .WithArguments($"--recurse-subdirectories --symbols \"{dotnetPath}/dotnet\" \"{dotnetPath}/lib*.so\""),
                cancellationToken)
                .ConfigureAwait(false);

            await DotNetCliCommandExecutor.ExecuteAsync(cliCommand
                .WithArguments($"tool uninstall dotnet-symbol --tool-path \"{toolPath}\""),
                cancellationToken)
                .ConfigureAwait(false);
        }

        private FileInfo GetTraceFile(DiagnoserActionParameters parameters, string extension)
            => new(ArtifactFileNameHelper.GetTraceFilePath(parameters, creationTime, extension)
                    .Replace(" ", "_")); // perfcollect does not allow for spaces in the trace file name
    }
}
