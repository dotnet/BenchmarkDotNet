using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Running
{
    public class MethodInvoker
    {
        private const int MinInvokeCount = 4;
        private const int MinIterationTimeMs = 200;
        private const int WarmupAutoMinIterationCount = 5;
        private const int TargetAutoMinIterationCount = 10;
        private const double TargetIdleAutoMaxAcceptableError = 0.05;
        private const double TargetMainAutoMaxAcceptableError = 0.01;

        private struct MultiInvokeInput
        {
            public IterationMode IterationMode { get; }
            public int Index { get; }
            public long InvokeCount { get; }

            public MultiInvokeInput(IterationMode iterationMode, int index, long invokeCount)
            {
                IterationMode = iterationMode;
                Index = index;
                InvokeCount = invokeCount;
            }
        }

        public void Invoke(IJob job, long operationsPerInvoke, Action setupAction, Action targetAction, Action idleAction)
        {
            // Jitting
            setupAction();
            targetAction();
            idleAction();

            // Run
            Func<MultiInvokeInput, Measurement> multiInvoke = input =>
            {
                var action = input.IterationMode.IsOneOf(IterationMode.IdleWarmup, IterationMode.IdleTarget)
                    ? idleAction
                    : targetAction;
                return MultiInvoke(input.IterationMode, input.Index, setupAction, action, input.InvokeCount,
                    operationsPerInvoke);
            };
            Invoke(job, multiInvoke);
        }

        public void Invoke<T>(IJob job, long operationsPerInvoke, Action setupAction, Func<T> targetAction, Func<T> idleAction)
        {
            // Jitting
            setupAction();
            targetAction();
            idleAction();

            // Run
            Func<MultiInvokeInput, Measurement> multiInvoke = input =>
            {
                var action = input.IterationMode.IsOneOf(IterationMode.IdleWarmup, IterationMode.IdleTarget)
                    ? idleAction
                    : targetAction;
                return MultiInvoke(input.IterationMode, input.Index, setupAction, action, input.InvokeCount,
                    operationsPerInvoke);
            };
            Invoke(job, multiInvoke);
        }

        private void Invoke(IJob job, Func<MultiInvokeInput, Measurement> multiInvoke)
        {
            long invokeCount = 1;
            IList<Measurement> idle = null;
            if (job.Mode == Mode.Throughput)
            {
                invokeCount = RunPilot(multiInvoke, job.IterationTime);
                RunWarmup(multiInvoke, invokeCount, IterationMode.IdleWarmup, Count.Auto);
                idle = RunTarget(multiInvoke, invokeCount, IterationMode.IdleTarget, Count.Auto);
            }

            RunWarmup(multiInvoke, invokeCount, IterationMode.MainWarmup, job.WarmupCount);
            var main = RunTarget(multiInvoke, invokeCount, IterationMode.MainTarget, job.TargetCount);

            PrintResult(idle, main);

            RunDiagnostic(multiInvoke, invokeCount);
        }


        private static long RunPilot(Func<MultiInvokeInput, Measurement> multiInvoke, Count iterationTime)
        {
            long invokeCount = MinInvokeCount;
            if (iterationTime.IsAuto)
            {
                var stopwatchResolution = EnvironmentHelper.GetCurrentInfo().GetStopwatchResolution();
                int iterationCounter = 0;
                while (true)
                {
                    iterationCounter++;
                    var measurement = multiInvoke(new MultiInvokeInput(IterationMode.Pilot, iterationCounter, invokeCount));
                    if (stopwatchResolution / invokeCount <
                        measurement.GetAverageNanoseconds() * TargetMainAutoMaxAcceptableError &&
                        measurement.Nanoseconds > TimeUnit.Convert(MinIterationTimeMs, TimeUnit.Millisecond, TimeUnit.Nanoseconds))
                        break;
                    invokeCount *= 2;
                }
            }
            else
            {
                int iterationCounter = 0;
                int downCount = 0;
                while (true)
                {
                    iterationCounter++;
                    var measurement = multiInvoke(new MultiInvokeInput(IterationMode.Pilot, iterationCounter, invokeCount));
                    var newInvokeCount = Math.Max(5, (long)Math.Round(invokeCount * iterationTime / measurement.Nanoseconds));
                    if (newInvokeCount < invokeCount)
                        downCount++;
                    if (Math.Abs(newInvokeCount - invokeCount) <= 1 || downCount >= 3)
                        break;
                    invokeCount = newInvokeCount;
                }
            }
            Console.WriteLine();
            return invokeCount;
        }

        private static void RunWarmup(Func<MultiInvokeInput, Measurement> multiInvoke, long invokeCount, IterationMode iterationMode, Count iterationCount)
        {
            if (iterationCount.IsAuto)
            {
                int iterationCounter = 0;
                Measurement previousMeasurement = null;
                int upCount = 0;
                while (true)
                {
                    iterationCounter++;
                    var measurement = multiInvoke(new MultiInvokeInput(iterationMode, iterationCounter, invokeCount));
                    if (previousMeasurement != null && measurement.Nanoseconds > previousMeasurement.Nanoseconds - 0.1)
                        upCount++;
                    if (iterationCounter >= WarmupAutoMinIterationCount && upCount >= 3)
                        break;
                    previousMeasurement = measurement;
                }
            }
            else
            {
                for (int i = 0; i < iterationCount; i++)
                    multiInvoke(new MultiInvokeInput(IterationMode.MainWarmup, i + 1, invokeCount));
            }
            Console.WriteLine();
        }

        private static IList<Measurement> RunTarget(Func<MultiInvokeInput, Measurement> multiInvoke, long invokeCount, IterationMode iterationMode, Count iterationCount)
        {
            var measurements = new List<Measurement>();
            if (iterationCount.IsAuto)
            {
                int iterationCounter = 0;
                var maxAcceptableError = iterationMode.IsOneOf(IterationMode.IdleWarmup, IterationMode.IdleTarget)
                    ? TargetIdleAutoMaxAcceptableError
                    : TargetMainAutoMaxAcceptableError;
                while (true)
                {
                    iterationCounter++;
                    var measurement = multiInvoke(new MultiInvokeInput(iterationMode, iterationCounter, invokeCount));
                    measurements.Add(measurement);
                    var statistics = new Statistics(measurements.Select(m => m.Nanoseconds));
                    var valuesWithoutOutliers = statistics.WithoutOutliers();
                    var statisticsWithoutOutliers = new Statistics(valuesWithoutOutliers);
                    if (valuesWithoutOutliers.Length >= TargetAutoMinIterationCount &&
                        statisticsWithoutOutliers.StandardError < maxAcceptableError * statisticsWithoutOutliers.Mean)
                        break;
                }
            }
            else
            {
                for (int i = 0; i < iterationCount; i++)
                    measurements.Add(multiInvoke(new MultiInvokeInput(IterationMode.MainTarget, i + 1, invokeCount)));
            }
            Console.WriteLine();
            return measurements;
        }

        private static void PrintResult(IList<Measurement> idle, IList<Measurement> main)
        {
            var overhead = idle == null ? 0.0 : new Statistics(idle.Select(m => m.Nanoseconds)).Median;
            var mainStatistics = new Statistics(main.Select(m => m.Nanoseconds));
            int resultIndex = 0;
            foreach (var measurement in main.Where(m => !mainStatistics.IsOutlier(m.Nanoseconds)))
            {
                var resultMeasurement = new Measurement(
                    measurement.ProcessIndex,
                    IterationMode.Result,
                    ++resultIndex,
                    measurement.Operations,
                    Math.Max(0, measurement.Nanoseconds - overhead));
                Console.WriteLine(resultMeasurement.ToOutputLine());
            }
            Console.WriteLine();
        }

        private static void RunDiagnostic(Func<MultiInvokeInput, Measurement> multiInvoke, long invokeCount)
        {
            multiInvoke(new MultiInvokeInput(IterationMode.Diagnostic, 1, invokeCount));
            Console.WriteLine();
        }

        private Measurement MultiInvoke(IterationMode mode, int index, Action setupAction, Action targetAction, long invocationCount, long operationsPerInvoke)
        {
            State.Instance.IterationMode = mode;
            State.Instance.Iteration = index;

            var totalOperations = invocationCount * operationsPerInvoke;
            setupAction();
            var stopwatch = new Stopwatch();
            GcCollect();
            if (invocationCount == 1)
            {
                stopwatch.Start();
                targetAction();
                stopwatch.Stop();
            }
            else if (invocationCount < int.MaxValue)
            {
                int intInvocationCount = (int)invocationCount;
                stopwatch.Start();
                for (int i = 0; i < intInvocationCount; i++)
                {
                    State.Instance.Operation = i;
                    targetAction();
                }
                stopwatch.Stop();
            }
            else
            {
                stopwatch.Start();
                for (long i = 0; i < invocationCount; i++)
                {
                    State.Instance.Operation = i;
                    targetAction();
                }
                stopwatch.Stop();
            }
            var measurement = Measurement.FromTicks(0, mode, index, totalOperations, stopwatch.ElapsedTicks);
            Console.WriteLine(measurement.ToOutputLine());
            GcCollect();
            return measurement;
        }

        private object multiInvokeReturnHolder;

        private Measurement MultiInvoke<T>(IterationMode mode, int index, Action setupAction, Func<T> targetAction, long invocationCount, long operationsPerInvoke, T returnHolder = default(T))
        {
            State.Instance.IterationMode = mode;
            State.Instance.Iteration = index;

            var totalOperations = invocationCount * operationsPerInvoke;
            setupAction();
            var stopwatch = new Stopwatch();
            GcCollect();
            if (invocationCount == 1)
            {
                stopwatch.Start();
                returnHolder = targetAction();
                stopwatch.Stop();
            }
            else if (invocationCount < int.MaxValue)
            {
                int intInvocationCount = (int)invocationCount;
                stopwatch.Start();
                for (int i = 0; i < intInvocationCount; i++)
                {
                    State.Instance.Operation = i;
                    returnHolder = targetAction();
                }
                stopwatch.Stop();
            }
            else
            {
                stopwatch.Start();
                for (long i = 0; i < invocationCount; i++)
                {
                    State.Instance.Operation = i;
                    returnHolder = targetAction();
                }
                stopwatch.Stop();
            }
            multiInvokeReturnHolder = returnHolder;
            var measurement = Measurement.FromTicks(0, mode, index, totalOperations, stopwatch.ElapsedTicks);
            Console.WriteLine(measurement.ToOutputLine());
            GcCollect();
            return measurement;
        }

        private static void GcCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}