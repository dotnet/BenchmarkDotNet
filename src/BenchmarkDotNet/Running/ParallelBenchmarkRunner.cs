using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace BenchmarkDotNet.Running
{
    internal class ParallelBenchmarkRunner
    {
        internal static void Run(BenchmarkRunInfo[] benchmarks,
            Dictionary<BenchmarkCase, (BenchmarkId benchmarkId, BuildResult buildResult)> buildResults,
            IResolver resolver,
            ILogger logger,
            List<string> artifactsToCleanup,
            string resultsFolderPath,
            string logFilePath)
        {
            var (queue, maxDegreeOfParallelism) = GetBenchmarksInOrder(benchmarks, buildResults, artifactsToCleanup);
            if (queue.IsEmpty)
                return;

            long counter = queue.Count;
            using Process currentProcess = Process.GetCurrentProcess();
            if (!currentProcess.TrySetAffinity(new IntPtr(1), logger))
            {
                logger.WriteError("Failed to set affinity for the current process, please read error message provided above and most likely re-run as admin/sudo.");
                return;
            }

            ConcurrentDictionary<Process, (BenchmarkCase benchmark, IntPtr coreId)> processToBenchmark = new ();
            for (int coreId = 1; coreId <= maxDegreeOfParallelism; coreId++)
            {
                StartNewProcess(new IntPtr(1 << coreId));
            }

            while (Interlocked.Read(ref counter) != 0)
            {
                Thread.Sleep(3);
            }

            void StartNewProcess(IntPtr coreId)
            {
                if (queue.TryDequeue(out var data))
                {
                    var toolchain = data.benchmark.GetToolchain();

                    var processStartInfo = toolchain.Executor.GetProcessStartInfo(
                        new ExecuteParameters(
                            data.buildResult,
                            data.benchmark,
                            data.id,
                            logger,
                            resolver,
                            1,
                            diagnoser: null)); // custom diagnosers not supported TODO: extend config validation

                    processStartInfo.RedirectStandardInput = false;
                    processStartInfo.RedirectStandardOutput = false;
                    processStartInfo.RedirectStandardError = false;

                    processStartInfo.SetEnvironmentVariables(data.benchmark, resolver);

                    Process process = Process.Start(processStartInfo);
                    processToBenchmark.TryAdd(process, (data.benchmark, coreId));
                    process.EnableRaisingEvents = true; // required by process.Exited
                    logger.WriteLine($"{data.benchmark.DisplayInfo} has started on core " +
                        $"{Convert.ToString(coreId.ToInt32(), 2).PadLeft(Environment.ProcessorCount).Replace(" ", "0")}!");
                    process.Start();

                    process.TrySetAffinity(coreId, logger); // TODO: handle failures
                    process.EnsureHighPriority(logger);

                    process.Exited += Process_Exited;
                }
            }

            void Process_Exited(object sender, EventArgs e)
            {
                Process process = (Process)sender;
                var data = processToBenchmark[process];
                BenchmarkCase benchmark = data.benchmark;
                IntPtr coreId = data.coreId;
                logger.WriteLine($"{benchmark.DisplayInfo} has exited!");
                Interlocked.Decrement(ref counter);
                StartNewProcess(coreId);
            }
        }

        private static (ConcurrentQueue<(BenchmarkCase benchmark, BenchmarkId id, BuildResult buildResult)> queue, int maxDegreeOfParallelism)
            GetBenchmarksInOrder(BenchmarkRunInfo[] benchmarks, Dictionary<BenchmarkCase, (BenchmarkId benchmarkId, BuildResult buildResult)> buildResults, List<string> artifactsToCleanup)
        {
            ConcurrentQueue<(BenchmarkCase benchmark, BenchmarkId id, BuildResult buildResult)> queue = new ();
            int maxDegreeOfParallelism = 1;

            foreach (var benchmarkRunInfo in benchmarks.Where(info => info.Config.MaxDegreeOfParallelism > 1))
            foreach (var benchmarkCase in benchmarkRunInfo.BenchmarksCases)
            {
                var config = benchmarkRunInfo.Config;
                var info = buildResults[benchmarkCase];
                var buildResult = info.buildResult;

                if (buildResult.IsBuildSuccess)
                {
                    // TODO: handle LaunchCount
                    queue.Enqueue((benchmarkCase, info.benchmarkId, buildResult));
                    maxDegreeOfParallelism = Math.Max(maxDegreeOfParallelism, config.MaxDegreeOfParallelism);

                    if (!config.Options.IsSet(ConfigOptions.KeepBenchmarkFiles))
                        artifactsToCleanup.AddRange(buildResult.ArtifactsToCleanup);
                }
            }

            return (queue, maxDegreeOfParallelism);
        }
    }
}
