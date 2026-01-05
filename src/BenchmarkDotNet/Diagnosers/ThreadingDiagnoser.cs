using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

#nullable enable

namespace BenchmarkDotNet.Diagnosers
{
    public class ThreadingDiagnoser(ThreadingDiagnoserConfig config) : IInProcessDiagnoser
    {
        public static readonly ThreadingDiagnoser Default = new(new ThreadingDiagnoserConfig(displayCompletedWorkItemCountWhenZero: true, displayLockContentionWhenZero: true));

        private readonly Dictionary<BenchmarkCase, (long completedWorkItemCount, long lockContentionCount)> results = [];

        public ThreadingDiagnoserConfig Config { get; } = config;

        public IEnumerable<string> Ids => [nameof(ThreadingDiagnoser)];

        public IEnumerable<IExporter> Exporters => [];

        public IEnumerable<IAnalyser> Analysers => [];

        public void DisplayResults(ILogger logger) { }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.ExtraIteration;

        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults diagnoserResults)
        {
            if (results.TryGetValue(diagnoserResults.BenchmarkCase, out var counts))
            {
                double totalOperations = diagnoserResults.Measurements.First(m => m.IterationStage == IterationStage.Extra).Operations;
                yield return new Metric(new CompletedWorkItemCountMetricDescriptor(Config), counts.completedWorkItemCount / totalOperations);
                yield return new Metric(new LockContentionCountMetricDescriptor(Config), counts.lockContentionCount / totalOperations);
            }
        }

        public async IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
        {
            foreach (var benchmark in validationParameters.Benchmarks)
            {
                var runtime = benchmark.Job.ResolveValue(EnvironmentMode.RuntimeCharacteristic, EnvironmentResolver.Instance);

                if (runtime != null && runtime.RuntimeMoniker < RuntimeMoniker.NetCoreApp30)
                {
                    yield return new ValidationError(true, $"{nameof(ThreadingDiagnoser)} supports only .NET Core 3.0+", benchmark);
                }
            }
        }

        void IInProcessDiagnoser.DeserializeResults(BenchmarkCase benchmarkCase, string serializedResults)
        {
            var splitResults = serializedResults.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            var completedWorkItemCount = long.Parse(splitResults[0]);
            var lockContentionCount = long.Parse(splitResults[1]);
            results.Add(benchmarkCase, (completedWorkItemCount, lockContentionCount));
        }

        InProcessDiagnoserHandlerData IInProcessDiagnoser.GetHandlerData(BenchmarkCase benchmarkCase)
            => new(typeof(ThreadingDiagnoserInProcessHandler), null);

        internal class CompletedWorkItemCountMetricDescriptor(ThreadingDiagnoserConfig config) : IMetricDescriptor
        {
            public string Id => "CompletedWorkItemCount";
            public string DisplayName => Column.CompletedWorkItems;
            public string Legend => "The number of work items that have been processed in ThreadPool (per single operation)";
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric)
                => config?.DisplayCompletedWorkItemCountWhenZero == true || metric.Value > 0;
        }

        internal class LockContentionCountMetricDescriptor(ThreadingDiagnoserConfig config) : IMetricDescriptor
        {
            public string Id => "LockContentionCount";
            public string DisplayName => Column.LockContentions;
            public string Legend => "The number of times there was contention upon trying to take a Monitor's lock (per single operation)";
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric)
                => config?.DisplayLockContentionWhenZero == true || metric.Value > 0;
        }
    }

    [UsedImplicitly]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ThreadingDiagnoserInProcessHandler : IInProcessDiagnoserHandler
    {
#if NETSTANDARD2_0
        // BDN targets .NET Standard 2.0, these properties are not part of .NET Standard 2.0, were added in .NET Core 3.0
        private static readonly Func<long> GetCompletedWorkItemCountDelegate = CreateGetterDelegate(typeof(ThreadPool), nameof(CompletedWorkItemCount));
        private static readonly Func<long> GetLockContentionCountDelegate = CreateGetterDelegate(typeof(Monitor), nameof(LockContentionCount));
#endif
        public long CompletedWorkItemCount { get; set; }
        public long LockContentionCount { get; set; }

        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        void IInProcessDiagnoserHandler.Handle(BenchmarkSignal signal, InProcessDiagnoserActionArgs args)
        {
            switch (signal)
            {
                case BenchmarkSignal.BeforeExtraIteration:
                    ReadInitial();
                    break;
                case BenchmarkSignal.AfterExtraIteration:
                    ReadFinal();
                    break;
            }
        }

        void IInProcessDiagnoserHandler.Initialize(string? serializedConfig) { }

        string IInProcessDiagnoserHandler.SerializeResults() => $"{CompletedWorkItemCount} {LockContentionCount}";

        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        private void ReadInitial()
        {
#if NETSTANDARD2_0
            LockContentionCount = GetLockContentionCountDelegate(); // Monitor.LockContentionCount can schedule a work item and needs to be called before ThreadPool.CompletedWorkItemCount
            CompletedWorkItemCount = GetCompletedWorkItemCountDelegate();
#else
            LockContentionCount = Monitor.LockContentionCount;
            CompletedWorkItemCount = ThreadPool.CompletedWorkItemCount;
#endif
        }

        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        private void ReadFinal()
        {
#if NETSTANDARD2_0
            LockContentionCount = GetLockContentionCountDelegate() - LockContentionCount; // Monitor.LockContentionCount can schedule a work item and needs to be called before ThreadPool.CompletedWorkItemCount
            CompletedWorkItemCount = GetCompletedWorkItemCountDelegate() - CompletedWorkItemCount;
#else
            LockContentionCount = Monitor.LockContentionCount - LockContentionCount;
            CompletedWorkItemCount = ThreadPool.CompletedWorkItemCount - CompletedWorkItemCount;
#endif
        }

#if NETSTANDARD2_0
        private static Func<long> CreateGetterDelegate(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName);

            // we create delegate to avoid boxing, IMPORTANT!
            return property != null
                ? (Func<long>) property.GetGetMethod()!.CreateDelegate(typeof(Func<long>))
                : () => 0;
        }
#endif
    }
}