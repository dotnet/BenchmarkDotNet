using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.TestAdapter.Remoting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace BenchmarkDotNet.TestAdapter
{
    /// <summary>
    /// A class used for executing benchmarks
    /// </summary>
    internal class BenchmarkExecutor
    {
        // Gets FieldInfo of ImmutableConfig's loggers.
        private static readonly FieldInfo? loggersField = typeof(ImmutableConfig).GetField("loggers", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly CancellationTokenSource cts = new ();

        /// <summary>
        /// Runs all the benchmarks in the given assembly, updating the TestExecutionRecorder as they get run.
        /// </summary>
        /// <param name="assemblyPath">The dll or exe of the benchmark project.</param>
        /// <param name="recorder">The interface used to record the current test execution progress.</param>
        /// <param name="benchmarkIds">
        /// An optional list of benchmark IDs specifying which benchmarks to run.
        /// These IDs are the same as the ones generated for the VSTest TestCase.
        /// </param>
        public void RunBenchmarks(string assemblyPath, TestExecutionRecorderWrapper recorder, HashSet<Guid>? benchmarkIds = null)
        {
            var benchmarks = BenchmarkEnumerator.GetBenchmarksFromAssemblyPath(assemblyPath);
            var testCases = new List<TestCase>();

            var filteredBenchmarks = new List<BenchmarkRunInfo>();
            foreach (var benchmark in benchmarks)
            {
                var needsJobInfo = benchmark.BenchmarksCases.Select(c => c.Job.DisplayInfo).Distinct().Count() > 1;
                var filteredCases = new List<BenchmarkCase>();
                foreach (var benchmarkCase in benchmark.BenchmarksCases)
                {
                    var testId = benchmarkCase.GetTestCaseId();
                    if (benchmarkIds == null || benchmarkIds.Contains(testId))
                    {
                        filteredCases.Add(benchmarkCase);
                        testCases.Add(benchmarkCase.ToVsTestCase(assemblyPath, needsJobInfo));
                    }
                }

                if (filteredCases.Count > 0)
                {
                    filteredBenchmarks.Add(new BenchmarkRunInfo(filteredCases.ToArray(), benchmark.Type, benchmark.Config));
                }
            }

            benchmarks = filteredBenchmarks.ToArray();

            if (benchmarks.Length == 0)
                return;

            // Create an event processor which will subscribe to events and push them to VSTest
            var eventProcessor = new VsTestEventProcessor(testCases, recorder, cts.Token);

            // Create a logger which will forward all log messages in BDN to the VSTest logger.
            var logger = new VsTestLogger(recorder.GetLogger());

            // Modify all the benchmarks so that the event process and logger is added.
            benchmarks = benchmarks
                .Select(b =>
                {
                    ImmutableConfig config = b.Config.AddEventProcessor(eventProcessor).AddLogger(logger).CreateImmutableConfig();

                    // Remove console logger from ImmutableCofig to fix duplicated console logs are outputted issue.
                    if (loggersField != null && loggersField.DeclaringType == typeof(ImmutableHashSet))
                    {
                        var loggers = config.GetLoggers()
                                            .Where(x => x is not ConsoleLogger)
                                            .ToImmutableHashSet();
                        loggersField.SetValue(config, loggers);
                    }

                    return new BenchmarkRunInfo(b.BenchmarksCases, b.Type, config);
                })
                .ToArray();

            // Run all the benchmarks, and ensure that any tests that don't have a result yet are sent.
            BenchmarkRunner.Run(benchmarks);
            eventProcessor.SendUnsentTestResults();
        }

        /// <summary>
        /// Stop the benchmarks when next able.
        /// </summary>
        public void Cancel()
        {
            cts.Cancel();
        }
    }
}
