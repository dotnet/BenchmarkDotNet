using System.IO;
using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Mathematics;

namespace BenchmarkDotNet.ConsoleArguments
{
    internal class OptionHandler
    {
        public CommandLineOptions Options { get; set; }

        public FileInfo AssemblyFile { get; set; }

        public void Init(
            FileInfo assemblyFile,
            string job,
            string[] runtimes,
            string[] exporters,
            bool memory,
            bool disasm,
            string profiler,
            string[] filter,
            bool inProcess,
            DirectoryInfo artifacts,
            OutlierMode outliers,
            int? affinity,
            bool allStats,
            string[] allCategories,
            string[] anyCategories,
            string[] attribute,
            bool join,
            bool keepFiles,
            bool noOverwrite,
            string[] counters,
            FileInfo cli,
            DirectoryInfo packages,
            FileInfo[] coreRun,
            FileInfo monoPath,
            string clrVersion,
            string coreRtVersion,
            DirectoryInfo ilcPath,
            int? launchCount,
            int? warmupCount,
            int? minWarmupCount,
            int? maxWarmupCount,
            int? iterationTime,
            int? iterationCount,
            int? minIterationCount,
            int? maxIterationCount,
            int? invocationCount,
            int? unrollFactor,
            RunStrategy? strategy,
            bool runOncePerIteration,
            bool info,
            ListBenchmarkCaseMode list,
            int disasmDepth,
            bool disasmDiff,
            int? buildTimeout,
            bool stopOnFirstError,
            string statisticalTest)
        {
            AssemblyFile = assemblyFile;
            Options = new CommandLineOptions
            {
                BaseJob = job,
                Runtimes = runtimes ?? new string[]{},
                Exporters = exporters ?? new string[] { },
                UseMemoryDiagnoser = memory,
                UseDisassemblyDiagnoser = disasm,
                Profiler = profiler,
                Filters = filter ?? new string[] { },
                RunInProcess = inProcess,
                ArtifactsDirectory = artifacts,
                Outliers = outliers,
                Affinity = affinity,
                DisplayAllStatistics = allStats,
                AllCategories = allCategories ?? new string[] { },
                AnyCategories = anyCategories ?? new string[] { },
                AttributeNames = attribute ?? new string[] { },
                Join = join,
                KeepBenchmarkFiles = keepFiles,
                DontOverwriteResults = noOverwrite,
                HardwareCounters = counters ?? new string[] { },
                CliPath = cli,
                RestorePath = packages,
                CoreRunPaths = coreRun ?? new FileInfo[] { },
                MonoPath = monoPath,
                ClrVersion = clrVersion,
                CoreRtVersion = coreRtVersion,
                CoreRtPath = ilcPath,
                LaunchCount = launchCount,
                WarmupIterationCount = warmupCount,
                MinWarmupIterationCount = minWarmupCount,
                MaxWarmupIterationCount = maxWarmupCount,
                IterationTimeInMilliseconds = iterationTime,
                IterationCount = iterationCount,
                MinIterationCount = minIterationCount,
                MaxIterationCount = maxIterationCount,
                InvocationCount = invocationCount,
                UnrollFactor = unrollFactor,
                RunStrategy = strategy,
                RunOncePerIteration = runOncePerIteration,
                PrintInformation = info,
                ListBenchmarkCaseMode = list,
                DisassemblerRecursiveDepth = disasmDepth,
                DisassemblerDiff = disasmDiff,
                TimeOutInSeconds = buildTimeout,
                StopOnFirstError = stopOnFirstError,
                StatisticalTestThreshold = statisticalTest
            };
        }
    }
}