using BenchmarkDotNet.EventProcessors;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.TestAdapter.Remoting;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Perfolizer.Mathematics.Histograms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace BenchmarkDotNet.TestAdapter
{
    /// <summary>
    /// An event processor which will pass on benchmark execution information to VSTest.
    /// </summary>
    internal class VsTestEventProcessor : EventProcessor
    {
        private readonly Dictionary<Guid, TestCase> cases;
        private readonly TestExecutionRecorderWrapper recorder;
        private readonly CancellationToken cancellationToken;
        private readonly Stopwatch runTimerStopwatch = new ();
        private readonly Dictionary<Guid, TestResult> testResults = new ();
        private readonly HashSet<Guid> sentTestResults = new ();

        public VsTestEventProcessor(
            List<TestCase> cases,
            TestExecutionRecorderWrapper recorder,
            CancellationToken cancellationToken)
        {
            this.cases = cases.ToDictionary(c => c.Id);
            this.recorder = recorder;
            this.cancellationToken = cancellationToken;
        }

        public override void OnValidationError(ValidationError validationError)
        {
            // If the error is not linked to a benchmark case, then set the error on all benchmarks
            var errorCases = validationError.BenchmarkCase == null
                ? cases.Values.ToList()
                : new List<TestCase> { cases[validationError.BenchmarkCase.GetTestCaseId()] };
            foreach (var testCase in errorCases)
            {
                var testResult = GetOrCreateTestResult(testCase);

                if (validationError.IsCritical)
                {
                    // Fail if there is a critical validation error
                    testResult.Outcome = TestOutcome.Failed;

                    // Append validation error message to end of test case error message
                    testResult.ErrorMessage = testResult.ErrorMessage == null
                        ? validationError.Message
                        : $"{testResult.ErrorMessage}\n{validationError.Message}";

                    // The test result is not sent yet, in case there are multiple validation errors that need to be sent.
                }
                else
                {
                    // If the validation error is not critical, append it as a message
                    testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, $"WARNING: {validationError.Message}\n"));
                }
            }
        }

        public override void OnBuildComplete(BuildPartition buildPartition, BuildResult buildResult)
        {
            // Only need to handle build failures
            if (!buildResult.IsBuildSuccess)
            {
                foreach (var benchmarkBuildInfo in buildPartition.Benchmarks)
                {
                    var testCase = cases[benchmarkBuildInfo.BenchmarkCase.GetTestCaseId()];
                    var testResult = GetOrCreateTestResult(testCase);

                    if (buildResult.GenerateException != null)
                        testResult.ErrorMessage = $"// Generate Exception: {buildResult.GenerateException.Message}";
                    else if (!buildResult.IsBuildSuccess && buildResult.TryToExplainFailureReason(out string reason))
                        testResult.ErrorMessage = $"// Build Error: {reason}";
                    else if (buildResult.ErrorMessage != null)
                        testResult.ErrorMessage = $"// Build Error: {buildResult.ErrorMessage}";
                    testResult.Outcome = TestOutcome.Failed;

                    // Send the result immediately
                    RecordStart(testCase);
                    RecordEnd(testCase, testResult.Outcome);
                    RecordResult(testResult);
                    sentTestResults.Add(testCase.Id);
                }
            }
        }

        public override void OnStartRunBenchmark(BenchmarkCase benchmarkCase)
        {
            // TODO: add proper cancellation support to BDN so that we don't need to do cancellation through the event processor
            cancellationToken.ThrowIfCancellationRequested();

            var testCase = cases[benchmarkCase.GetTestCaseId()];
            var testResult = GetOrCreateTestResult(testCase);
            testResult.StartTime = DateTimeOffset.UtcNow;

            RecordStart(testCase);
            runTimerStopwatch.Restart();
        }

        public override void OnEndRunBenchmark(BenchmarkCase benchmarkCase, BenchmarkReport report)
        {
            var testCase = cases[benchmarkCase.GetTestCaseId()];
            var testResult = GetOrCreateTestResult(testCase);
            testResult.EndTime = DateTimeOffset.UtcNow;
            testResult.Duration = runTimerStopwatch.Elapsed;
            testResult.Outcome = report.Success ? TestOutcome.Passed : TestOutcome.Failed;

            var resultRuns = report.GetResultRuns();

            // Provide the raw result runs data.
            testResult.SetPropertyValue(VsTestProperties.Measurement, resultRuns.Select(m => m.Nanoseconds.ToString()).ToArray());

            // Add a message to the TestResult which contains the results summary.
            testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, report.BenchmarkCase.DisplayInfo + "\n"));
            testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, $"Runtime = {report.GetRuntimeInfo()}; GC = {report.GetGcInfo()}\n"));

            var statistics = resultRuns.GetStatistics();
            var cultureInfo = CultureInfo.InvariantCulture;
            var formatter = statistics.CreateNanosecondFormatter(cultureInfo);

            var builder = new StringBuilder();
            var histogram = HistogramBuilder.Adaptive.Build(statistics.Sample.Values);
            builder.AppendLine("-------------------- Histogram --------------------");
            builder.AppendLine(histogram.ToString(formatter));
            builder.AppendLine("---------------------------------------------------");

            var statisticsOutput = statistics.ToString(cultureInfo, formatter, calcHistogram: false);
            builder.AppendLine(statisticsOutput);

            testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, builder.ToString()));

            RecordEnd(testResult.TestCase, testResult.Outcome);
            RecordResult(testResult);
            sentTestResults.Add(testCase.Id);
        }

        /// <summary>
        /// Iterate through all the benchmarks that were scheduled to run, and if they haven't been sent yet, send the result through.
        /// </summary>
        public void SendUnsentTestResults()
        {
            foreach (var testCase in cases.Values)
            {
                if (!sentTestResults.Contains(testCase.Id))
                {
                    var testResult = GetOrCreateTestResult(testCase);
                    if (testResult.Outcome == TestOutcome.None)
                        testResult.Outcome = TestOutcome.Skipped;
                    RecordStart(testCase);
                    RecordEnd(testCase, testResult.Outcome);
                    RecordResult(testResult);
                }
            }
        }

        private TestResult GetOrCreateTestResult(TestCase testCase)
        {
            if (testResults.TryGetValue(testCase.Id, out var testResult))
                return testResult;

            var newResult = new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = testCase.DisplayName
            };

            testResults[testCase.Id] = newResult;
            return newResult;
        }

        private void RecordStart(TestCase testCase)
        {
            recorder.RecordStart(SerializationHelpers.Serialize(testCase));
        }

        private void RecordEnd(TestCase testCase, TestOutcome testOutcome)
        {
            recorder.RecordEnd(SerializationHelpers.Serialize(testCase), testOutcome);
        }

        private void RecordResult(TestResult testResult)
        {
            recorder.RecordResult(SerializationHelpers.Serialize(testResult));
        }
    }
}
