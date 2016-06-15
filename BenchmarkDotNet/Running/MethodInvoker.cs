using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using GC = BenchmarkDotNet.Jobs.GC;

namespace BenchmarkDotNet.Running
{
    public class MethodInvoker
    {
        private const int MinInvokeCount = 4;
        private const int MinIterationTimeMs = 200;
        private const int WarmupAutoMinIterationCount = 5;
        private const int TargetAutoMinIterationCount = 10;
        private const double TargetIdleAutoMaxAcceptableError = 0.05;
        private const double TargetIdleAutoMaxIterationCount = 30;
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
                    operationsPerInvoke, job.GC);
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
                    operationsPerInvoke, job.GC);
            };
            Invoke(job, multiInvoke);
        }

        public void Invoke<T>(IJob job, long operationsPerInvoke, Action setupAction, Func<T> targetAction, Func<int> idleAction)
        {
            // Jitting
            setupAction();
            targetAction();
            idleAction();

            // Run
            Func<MultiInvokeInput, Measurement> multiInvoke = input => input.IterationMode.IsOneOf(IterationMode.IdleWarmup, IterationMode.IdleTarget) ?
                MultiInvoke(input.IterationMode, input.Index, setupAction, idleAction, input.InvokeCount, operationsPerInvoke, job.GC) :
                MultiInvoke(input.IterationMode, input.Index, setupAction, targetAction, input.InvokeCount, operationsPerInvoke, job.GC);
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
        }

        private static long RunPilot(Func<MultiInvokeInput, Measurement> multiInvoke, Count iterationTime)
        {
            long invokeCount = MinInvokeCount;
            if (iterationTime.IsAuto)
            {
                var resolution = Chronometer.GetResolution();
                int iterationCounter = 0;
                while (true)
                {
                    iterationCounter++;
                    var measurement = multiInvoke(new MultiInvokeInput(IterationMode.Pilot, iterationCounter, invokeCount));
                    if (resolution / invokeCount <
                        measurement.GetAverageNanoseconds() * TargetMainAutoMaxAcceptableError &&
                        measurement.Nanoseconds > TimeUnit.Convert(MinIterationTimeMs, TimeUnit.Millisecond, TimeUnit.Nanoseconds))
                        break;
                    invokeCount *= 2;
                }
            }
            else
            {
                var iterationTimeInNanoseconds = TimeUnit.Convert(iterationTime, TimeUnit.Millisecond, TimeUnit.Nanoseconds);
                int iterationCounter = 0;
                int downCount = 0;
                while (true)
                {
                    iterationCounter++;
                    var measurement = multiInvoke(new MultiInvokeInput(IterationMode.Pilot, iterationCounter, invokeCount));
                    var newInvokeCount = Math.Max(5, (long)Math.Round(invokeCount * iterationTimeInNanoseconds / measurement.Nanoseconds));
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
                bool isIdle = iterationMode.IsOneOf(IterationMode.IdleWarmup, IterationMode.IdleTarget);
                var maxAcceptableError = isIdle ? TargetIdleAutoMaxAcceptableError : TargetMainAutoMaxAcceptableError;
                while (true)
                {
                    iterationCounter++;
                    var measurement = multiInvoke(new MultiInvokeInput(iterationMode, iterationCounter, invokeCount));
                    measurements.Add(measurement);
                    var statistics = new Statistics(measurements.Select(m => m.Nanoseconds));
                    var statisticsWithoutOutliers = new Statistics(statistics.WithoutOutliers());
                    if (iterationCounter >= TargetAutoMinIterationCount &&
                        statisticsWithoutOutliers.StandardError < maxAcceptableError * statisticsWithoutOutliers.Mean)
                        break;
                    if (isIdle && iterationCounter >= TargetIdleAutoMaxIterationCount)
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
            int resultIndex = 0;
            foreach (var measurement in main)
            {
                var resultMeasurement = new Measurement(
                    measurement.LaunchIndex,
                    IterationMode.Result,
                    ++resultIndex,
                    measurement.Operations,
                    Math.Max(0, measurement.Nanoseconds - overhead));
                Console.WriteLine(resultMeasurement.ToOutputLine());
            }
            Console.WriteLine();
        }

        private Measurement MultiInvoke(IterationMode mode, int index, Action setupAction, Action targetAction, long invocationCount, long operationsPerInvoke, GC gcSettings)
        {
            var totalOperations = invocationCount * operationsPerInvoke;
            setupAction();
            ClockSpan clockSpan;
            GcCollect(gcSettings);
            if (invocationCount == 1)
            {
                var chronometer = Chronometer.Start();
                targetAction();
                clockSpan = chronometer.Stop();
            }
            else if (invocationCount < int.MaxValue)
            {
                int intInvocationCount = (int)invocationCount;
                var chronometer = Chronometer.Start();
                RunAction(targetAction, intInvocationCount);
                clockSpan = chronometer.Stop();
            }
            else
            {
                var chronometer = Chronometer.Start();
                RunAction(targetAction, invocationCount);
                clockSpan = chronometer.Stop();
            }
            var measurement = new Measurement(0, mode, index, totalOperations, clockSpan.GetNanoseconds());
            Console.WriteLine(measurement.ToOutputLine());
            GcCollect(gcSettings);
            return measurement;
        }

        private object multiInvokeReturnHolder;

        private Measurement MultiInvoke<T>(IterationMode mode, int index, Action setupAction, Func<T> targetAction, long invocationCount, long operationsPerInvoke, GC gcSettings, T returnHolder = default(T))
        {
            var totalOperations = invocationCount * operationsPerInvoke;
            setupAction();
            ClockSpan clockSpan;
            GcCollect(gcSettings);
            if (invocationCount == 1)
            {
                var chronometer = Chronometer.Start();
                returnHolder = targetAction();
                clockSpan = chronometer.Stop();
            }
            else if (invocationCount < int.MaxValue)
            {
                int intInvocationCount = (int)invocationCount;
                var chronometer = Chronometer.Start();
                RunAction(targetAction, intInvocationCount);
                clockSpan = chronometer.Stop();
            }
            else
            {
                var chronometer = Chronometer.Start();
                RunAction(targetAction, invocationCount);
                clockSpan = chronometer.Stop();
            }
            multiInvokeReturnHolder = returnHolder;
            var measurement = new Measurement(0, mode, index, totalOperations, clockSpan.GetNanoseconds());
            Console.WriteLine(measurement.ToOutputLine());
            GcCollect(gcSettings);
            return measurement;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunAction(Action action, int count)
        {
            for (int i = 0; i < count; i++)
                action();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunAction(Action action, long count)
        {
            for (int i = 0; i < count; i++)
                action();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RunAction<T>(Func<T> action, int count, T returnHolder = default(T))
        {            
            for (int i = 0; i < count; i++)
                returnHolder = action();
            multiInvokeReturnHolder = returnHolder;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RunAction<T>(Func<T> action, long count, T returnHolder = default(T))
        {
            for (int i = 0; i < count; i++)
                returnHolder = action();
            multiInvokeReturnHolder = returnHolder;
        }

        private static void GcCollect(GC gcSettings)
        {
            if (gcSettings != null && !gcSettings.Force)
            {
                return;
            }

            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
        }
    }
}