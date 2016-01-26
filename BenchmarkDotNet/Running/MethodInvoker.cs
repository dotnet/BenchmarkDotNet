using System;
using System.Diagnostics;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Running
{
    // TODO: remove duplications
    public class MethodInvoker
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

        public void Throughput(IJob job, long operationsPerInvoke, Action setupAction, Action targetAction, Action idleAction)
        {
            int warmupCount = job.WarmupCount.IsAuto ? 5 : job.WarmupCount.Value; // TODO
            int targetCount = job.TargetCount.IsAuto ? 10 : job.TargetCount.Value; // TODO

            setupAction();
            targetAction();
            idleAction();

            long invokeCount = 1;
            double lastPilotMilliseconds = 0;
            int pilotCounter = 0;
            while (true)
            {
                State.Instance.IterationMode = IterationMode.Pilot;
                State.Instance.Iteration = pilotCounter++;
                var measurement = MultiInvoke(IterationMode.Pilot, pilotCounter - 1, setupAction, targetAction, invokeCount, operationsPerInvoke);
                lastPilotMilliseconds = measurement.Milliseconds;
                if (lastPilotMilliseconds > InvokeTimoutMilliseconds)
                    break;
                if (lastPilotMilliseconds < 1)
                    invokeCount *= InvokeTimoutMilliseconds;
                else
                    invokeCount *= (long)Math.Ceiling(InvokeTimoutMilliseconds / lastPilotMilliseconds);
            }
            double idleMilliseconds = 0;
            for (int i = 0; i < Math.Min(3, warmupCount); i++)
            {
                State.Instance.IterationMode = IterationMode.WarmupIdle;
                State.Instance.Iteration = i;
                var measurement = MultiInvoke(IterationMode.WarmupIdle, i, setupAction, idleAction, invokeCount, operationsPerInvoke);
                idleMilliseconds = measurement.Milliseconds;
            }
            invokeCount = invokeCount * 1000 / (long)Math.Round(Math.Min(1000, Math.Max(100, lastPilotMilliseconds - idleMilliseconds)));
            long idleTicks = 0;
            var targetIdleInvokeCount = Math.Min(5, targetCount);
            for (int i = 0; i < targetIdleInvokeCount; i++)
            {
                State.Instance.IterationMode = IterationMode.TargetIdle;
                State.Instance.Iteration = i;
                var measurement = MultiInvoke(IterationMode.TargetIdle, i, setupAction, idleAction, invokeCount, operationsPerInvoke);
                idleTicks += measurement.Ticks;
            }
            idleTicks /= targetIdleInvokeCount;

            for (int i = 0; i < warmupCount; i++)
            {
                State.Instance.IterationMode = IterationMode.Warmup;
                State.Instance.Iteration = i;
                MultiInvoke(IterationMode.Warmup, i, setupAction, targetAction, invokeCount, operationsPerInvoke, idleTicks);
            }
            for (int i = 0; i < targetCount; i++)
            {
                State.Instance.IterationMode = IterationMode.Target;
                State.Instance.Iteration = i;
                MultiInvoke(IterationMode.Target, i, setupAction, targetAction, invokeCount, operationsPerInvoke, idleTicks);
            }
        }

        public void Throughput<T>(IJob job, long operationsPerInvoke, Action setupAction, Func<T> targetAction, Func<T> idleAction)
        {
            int warmupCount = job.WarmupCount.IsAuto ? 5 : job.WarmupCount.Value; // TODO
            int targetCount = job.TargetCount.IsAuto ? 10 : job.TargetCount.Value; // TODO

            setupAction();
            targetAction();
            idleAction();

            long invokeCount = 1;
            double lastPilotMilliseconds = 0;
            int pilotCounter = 0;
            while (true)
            {
                State.Instance.IterationMode = IterationMode.Pilot;
                State.Instance.Iteration = pilotCounter++;
                var measurement = MultiInvoke(IterationMode.Pilot, pilotCounter - 1, setupAction, targetAction, invokeCount, operationsPerInvoke);
                lastPilotMilliseconds = measurement.Milliseconds;
                if (lastPilotMilliseconds > InvokeTimoutMilliseconds)
                    break;
                if (lastPilotMilliseconds < 1)
                    invokeCount *= InvokeTimoutMilliseconds;
                else
                    invokeCount *= (long)Math.Ceiling(InvokeTimoutMilliseconds / lastPilotMilliseconds);
            }
            double idleMilliseconds = 0;
            for (int i = 0; i < Math.Min(3, warmupCount); i++)
            {
                State.Instance.IterationMode = IterationMode.WarmupIdle;
                State.Instance.Iteration = i;
                var measurement = MultiInvoke(IterationMode.WarmupIdle, i, setupAction, idleAction, invokeCount, operationsPerInvoke);
                idleMilliseconds = measurement.Milliseconds;
            }
            invokeCount = invokeCount * 1000 / (long)Math.Round(Math.Min(1000, Math.Max(100, lastPilotMilliseconds - idleMilliseconds)));
            long idleTicks = 0;
            var targetIdleInvokeCount = Math.Min(5, targetCount);
            for (int i = 0; i < targetIdleInvokeCount; i++)
            {
                State.Instance.IterationMode = IterationMode.TargetIdle;
                State.Instance.Iteration = i;
                var measurement = MultiInvoke(IterationMode.TargetIdle, i, setupAction, idleAction, invokeCount, operationsPerInvoke);
                idleTicks += measurement.Ticks;
            }
            idleTicks /= targetIdleInvokeCount;

            for (int i = 0; i < warmupCount; i++)
            {
                State.Instance.IterationMode = IterationMode.Warmup;
                State.Instance.Iteration = i;
                MultiInvoke(IterationMode.Warmup, i, setupAction, targetAction, invokeCount, operationsPerInvoke, idleTicks);
            }
            for (int i = 0; i < targetCount; i++)
            {
                State.Instance.IterationMode = IterationMode.Target;
                State.Instance.Iteration = i;
                MultiInvoke(IterationMode.Target, i, setupAction, targetAction, invokeCount, operationsPerInvoke, idleTicks);
            }
        }

        public void SingleRun(IJob job, long operationsPerInvoke, Action setupAction, Action targetAction, Action idleAction)
        {
            int warmupCount = job.WarmupCount.IsAuto ? 5 : job.WarmupCount.Value; // TODO
            int targetCount = job.TargetCount.IsAuto ? 10 : job.TargetCount.Value; // TODO

            for (int i = 0; i < warmupCount; i++)
            {
                State.Instance.IterationMode = IterationMode.Warmup;
                State.Instance.Iteration = i;
                MultiInvoke(IterationMode.Warmup, i, setupAction, targetAction, 1, operationsPerInvoke);
            }
            for (int i = 0; i < targetCount; i++)
            {
                State.Instance.IterationMode = IterationMode.Target;
                State.Instance.Iteration = i;
                MultiInvoke(IterationMode.Target, i, setupAction, targetAction, 1, operationsPerInvoke);
            }
        }

        public void SingleRun<T>(IJob job, long operationsPerInvoke, Action setupAction, Func<T> targetAction, Func<T> idleAction)
        {
            int warmupCount = job.WarmupCount.IsAuto ? 5 : job.WarmupCount.Value; // TODO
            int targetCount = job.TargetCount.IsAuto ? 10 : job.TargetCount.Value; // TODO

            for (int i = 0; i < warmupCount; i++)
            {
                State.Instance.IterationMode = IterationMode.Warmup;
                State.Instance.Iteration = i;
                MultiInvoke(IterationMode.Warmup, i, setupAction, targetAction, 1, operationsPerInvoke);
            }
            for (int i = 0; i < targetCount; i++)
            {
                State.Instance.IterationMode = IterationMode.Target;
                State.Instance.Iteration = i;
                MultiInvoke(IterationMode.Target, i, setupAction, targetAction, 1, operationsPerInvoke);
            }
        }

        private Measurement MultiInvoke(IterationMode mode, int index, Action setupAction, Action targetAction, long invocationCount, long operationsPerInvoke, long idleTicks = 0)
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
            var measurement = new Measurement(totalOperations, stopwatch.ElapsedTicks - idleTicks);
            Console.WriteLine($"{mode} {index + 1}: {measurement.GetDisplayValue()}");
            GcCollect();
            return measurement;
        }

        private object multiInvokeReturnHolder;

        private Measurement MultiInvoke<T>(IterationMode mode, int index, Action setupAction, Func<T> targetAction, long invocationCount, long operationsPerInvoke, long idleTicks = 0, T returnHolder = default(T))
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