using System;
using System.Diagnostics;
using BenchmarkDotNet.Common;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet
{
    // TODO: remove duplications
    public class BenchmarkMethodInvoker
    {
        private const long InvokeTimoutMilliseconds = 1000; // TODO: Move to settings

        private class Measurement
        {
            public long OperationCount { get; }
            public long Ticks { get; }
            public double Nanoseconds { get; }
            public double Milliseconds => Nanoseconds / 1000000;

            public Measurement(long operationCount, long ticks)
            {
                OperationCount = operationCount;
                Ticks = Math.Max(ticks, 1);
                Nanoseconds = Ticks / (double)Stopwatch.Frequency * 1000000000;
            }

            public string GetDisplayValue() => $"{OperationCount} op, {Nanoseconds.ToStr()} ns, {GetAverageTime()}";
            private string GetAverageTime() => $"{(Nanoseconds / OperationCount).ToTimeStr()}/op";
        }

        public void Throughput(BenchmarkTask task, long operationsPerInvoke, Action setupAction, Action targetAction, Action idleAction)
        {
            setupAction();
            targetAction();
            idleAction();

            long invokeCount = 1;
            double lastPilotMilliseconds = 0;
            int pilotCounter = 0;
            while (true)
            {
                BenchmarkState.Instance.IterationMode = BenchmarkIterationMode.Pilot;
                BenchmarkState.Instance.Iteration = pilotCounter++;
                var measurement = MultiInvoke(BenchmarkIterationMode.Pilot, pilotCounter - 1, setupAction, targetAction, invokeCount, operationsPerInvoke);
                lastPilotMilliseconds = measurement.Milliseconds;
                if (lastPilotMilliseconds > InvokeTimoutMilliseconds)
                    break;
                if (lastPilotMilliseconds < 1)
                    invokeCount *= InvokeTimoutMilliseconds;
                else
                    invokeCount *= (long)Math.Ceiling(InvokeTimoutMilliseconds / lastPilotMilliseconds);
            }
            double idleMilliseconds = 0;
            for (int i = 0; i < Math.Min(3, task.Configuration.WarmupIterationCount); i++)
            {
                BenchmarkState.Instance.IterationMode = BenchmarkIterationMode.WarmupIdle;
                BenchmarkState.Instance.Iteration = i;
                var measurement = MultiInvoke(BenchmarkIterationMode.WarmupIdle, i, setupAction, idleAction, invokeCount, operationsPerInvoke);
                idleMilliseconds = measurement.Milliseconds;
            }
            invokeCount = invokeCount * 1000 / (long)Math.Round(Math.Min(1000, Math.Max(100, lastPilotMilliseconds - idleMilliseconds)));
            long idleTicks = 0;
            var targetIdleInvokeCount = Math.Min(5, task.Configuration.TargetIterationCount);
            for (int i = 0; i < targetIdleInvokeCount; i++)
            {
                BenchmarkState.Instance.IterationMode = BenchmarkIterationMode.TargetIdle;
                BenchmarkState.Instance.Iteration = i;
                var measurement = MultiInvoke(BenchmarkIterationMode.TargetIdle, i, setupAction, idleAction, invokeCount, operationsPerInvoke);
                idleTicks += measurement.Ticks;
            }
            idleTicks /= targetIdleInvokeCount;

            for (int i = 0; i < task.Configuration.WarmupIterationCount; i++)
            {
                BenchmarkState.Instance.IterationMode = BenchmarkIterationMode.Warmup;
                BenchmarkState.Instance.Iteration = i;
                MultiInvoke(BenchmarkIterationMode.Warmup, i, setupAction, targetAction, invokeCount, operationsPerInvoke, idleTicks);
            }
            for (int i = 0; i < task.Configuration.TargetIterationCount; i++)
            {
                BenchmarkState.Instance.IterationMode = BenchmarkIterationMode.Target;
                BenchmarkState.Instance.Iteration = i;
                MultiInvoke(BenchmarkIterationMode.Target, i, setupAction, targetAction, invokeCount, operationsPerInvoke, idleTicks);
            }
        }

        public void Throughput<T>(BenchmarkTask task, long operationsPerInvoke, Action setupAction, Func<T> targetAction, Func<T> idleAction)
        {
            setupAction();
            targetAction();
            idleAction();

            long invokeCount = 1;
            double lastPilotMilliseconds = 0;
            int pilotCounter = 0;
            while (true)
            {
                BenchmarkState.Instance.IterationMode = BenchmarkIterationMode.Pilot;
                BenchmarkState.Instance.Iteration = pilotCounter++;
                var measurement = MultiInvoke(BenchmarkIterationMode.Pilot, pilotCounter - 1, setupAction, targetAction, invokeCount, operationsPerInvoke);
                lastPilotMilliseconds = measurement.Milliseconds;
                if (lastPilotMilliseconds > InvokeTimoutMilliseconds)
                    break;
                if (lastPilotMilliseconds < 1)
                    invokeCount *= InvokeTimoutMilliseconds;
                else
                    invokeCount *= (long)Math.Ceiling(InvokeTimoutMilliseconds / lastPilotMilliseconds);
            }
            double idleMilliseconds = 0;
            for (int i = 0; i < Math.Min(3, task.Configuration.WarmupIterationCount); i++)
            {
                BenchmarkState.Instance.IterationMode = BenchmarkIterationMode.WarmupIdle;
                BenchmarkState.Instance.Iteration = i;
                var measurement = MultiInvoke(BenchmarkIterationMode.WarmupIdle, i, setupAction, idleAction, invokeCount, operationsPerInvoke);
                idleMilliseconds = measurement.Milliseconds;
            }
            invokeCount = invokeCount * 1000 / (long)Math.Round(Math.Min(1000, Math.Max(100, lastPilotMilliseconds - idleMilliseconds)));
            long idleTicks = 0;
            var targetIdleInvokeCount = Math.Min(5, task.Configuration.TargetIterationCount);
            for (int i = 0; i < targetIdleInvokeCount; i++)
            {
                BenchmarkState.Instance.IterationMode = BenchmarkIterationMode.TargetIdle;
                BenchmarkState.Instance.Iteration = i;
                var measurement = MultiInvoke(BenchmarkIterationMode.TargetIdle, i, setupAction, idleAction, invokeCount, operationsPerInvoke);
                idleTicks += measurement.Ticks;
            }
            idleTicks /= targetIdleInvokeCount;

            for (int i = 0; i < task.Configuration.WarmupIterationCount; i++)
            {
                BenchmarkState.Instance.IterationMode = BenchmarkIterationMode.Warmup;
                BenchmarkState.Instance.Iteration = i;
                MultiInvoke(BenchmarkIterationMode.Warmup, i, setupAction, targetAction, invokeCount, operationsPerInvoke, idleTicks);
            }
            for (int i = 0; i < task.Configuration.TargetIterationCount; i++)
            {
                BenchmarkState.Instance.IterationMode = BenchmarkIterationMode.Target;
                BenchmarkState.Instance.Iteration = i;
                MultiInvoke(BenchmarkIterationMode.Target, i, setupAction, targetAction, invokeCount, operationsPerInvoke, idleTicks);
            }
        }

        public void SingleRun(BenchmarkTask task, long operationsPerInvoke, Action setupAction, Action targetAction, Action idleAction)
        {
            for (int i = 0; i < task.Configuration.WarmupIterationCount; i++)
            {
                BenchmarkState.Instance.IterationMode = BenchmarkIterationMode.Warmup;
                BenchmarkState.Instance.Iteration = i;
                MultiInvoke(BenchmarkIterationMode.Warmup, i, setupAction, targetAction, 1, operationsPerInvoke);
            }
            for (int i = 0; i < task.Configuration.TargetIterationCount; i++)
            {
                BenchmarkState.Instance.IterationMode = BenchmarkIterationMode.Target;
                BenchmarkState.Instance.Iteration = i;
                MultiInvoke(BenchmarkIterationMode.Target, i, setupAction, targetAction, 1, operationsPerInvoke);
            }
        }

        public void SingleRun<T>(BenchmarkTask task, long operationsPerInvoke, Action setupAction, Func<T> targetAction, Func<T> idleAction)
        {
            for (int i = 0; i < task.Configuration.WarmupIterationCount; i++)
            {
                BenchmarkState.Instance.IterationMode = BenchmarkIterationMode.Warmup;
                BenchmarkState.Instance.Iteration = i;
                MultiInvoke(BenchmarkIterationMode.Warmup, i, setupAction, targetAction, 1, operationsPerInvoke);
            }
            for (int i = 0; i < task.Configuration.TargetIterationCount; i++)
            {
                BenchmarkState.Instance.IterationMode = BenchmarkIterationMode.Target;
                BenchmarkState.Instance.Iteration = i;
                MultiInvoke(BenchmarkIterationMode.Target, i, setupAction, targetAction, 1, operationsPerInvoke);
            }
        }

        private Measurement MultiInvoke(BenchmarkIterationMode mode, int index, Action setupAction, Action targetAction, long invocationCount, long operationsPerInvoke, long idleTicks = 0)
        {
            var totalOperations = invocationCount * operationsPerInvoke;
            setupAction();
            var stopwatch = new Stopwatch();
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
                    BenchmarkState.Instance.Operation = i;
                    targetAction();
                }
                stopwatch.Stop();
            }
            else
            {
                stopwatch.Start();
                for (long i = 0; i < invocationCount; i++)
                {
                    BenchmarkState.Instance.Operation = i;
                    targetAction();
                }
                stopwatch.Stop();
            }
            var measurement = new Measurement(totalOperations, stopwatch.ElapsedTicks - idleTicks);
            Console.WriteLine($"{mode} {index + 1}: {measurement.GetDisplayValue()}");
            GcCollect();
            return measurement;
        }

        private object multiInvokeReturnHolder;

        private Measurement MultiInvoke<T>(BenchmarkIterationMode mode, int index, Action setupAction, Func<T> targetAction, long invocationCount, long operationsPerInvoke, long idleTicks = 0, T returnHolder = default(T))
        {
            var totalOperations = invocationCount * operationsPerInvoke;
            setupAction();
            var stopwatch = new Stopwatch();
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
                    BenchmarkState.Instance.Operation = i;
                    returnHolder = targetAction();
                }
                stopwatch.Stop();
            }
            else
            {
                stopwatch.Start();
                for (long i = 0; i < invocationCount; i++)
                {
                    BenchmarkState.Instance.Operation = i;
                    returnHolder = targetAction();
                }
                stopwatch.Stop();
            }
            multiInvokeReturnHolder = returnHolder;
            var measurement = new Measurement(totalOperations, stopwatch.ElapsedTicks - idleTicks);
            Console.WriteLine($"{mode} {index + 1}: {measurement.GetDisplayValue()}");
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