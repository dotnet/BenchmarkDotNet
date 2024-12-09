using System;
using System.Collections.Generic;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Perfolizer.Json;

namespace BenchmarkDotNet.Diagnosers
{
    public class ThreadingDiagnoser : IDiagnoser
    {
        public static readonly ThreadingDiagnoser Default = new ThreadingDiagnoser(new ThreadingDiagnoserConfig(displayCompletedWorkItemCountWhenZero: true, displayLockContentionWhenZero: true));

        public ThreadingDiagnoser(ThreadingDiagnoserConfig config) => Config = config;
        public ThreadingDiagnoserConfig Config { get; }

        public IEnumerable<string> Ids => new[] { nameof(ThreadingDiagnoser) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public void DisplayResults(ILogger logger) { }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            var completedWorkItemCountDescriptor = CompletedWorkItemCountMetricDescriptor.Instance;
            if (completedWorkItemCountDescriptor is CompletedWorkItemCountMetricDescriptor concreteCompletedWorkItemCountDescriptor)
            {
                concreteCompletedWorkItemCountDescriptor.SetConfiguration(Config);
            }
            yield return new Metric(completedWorkItemCountDescriptor, results.ThreadingStats.CompletedWorkItemCount / (double)results.ThreadingStats.TotalOperations);


            var lockContentionCountDescriptor = LockContentionCountMetricDescriptor.Instance;
            if (lockContentionCountDescriptor is LockContentionCountMetricDescriptor concreteLockContentionCountDescriptor)
            {
                concreteLockContentionCountDescriptor.SetConfiguration(Config);
            }
            yield return new Metric(lockContentionCountDescriptor, results.ThreadingStats.LockContentionCount / (double)results.ThreadingStats.TotalOperations);
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            foreach (var benchmark in validationParameters.Benchmarks)
            {
                var runtime = benchmark.Job.ResolveValue(EnvironmentMode.RuntimeCharacteristic, EnvironmentResolver.Instance);

                if (runtime.RuntimeMoniker < RuntimeMoniker.NetCoreApp31)
                {
                    yield return new ValidationError(true, $"{nameof(ThreadingDiagnoser)} supports only .NET Core 3.1+", benchmark);
                }
            }
        }

        private class CompletedWorkItemCountMetricDescriptor : MetricDescriptorConfigurationHandler<ThreadingDiagnoserConfig>, IMetricDescriptor
        {
            public static IMetricDescriptor Instance
                => MetricDescriptorSingletonBase<CompletedWorkItemCountMetricDescriptor>.Instance;

            public string Id => "CompletedWorkItemCount";
            public string DisplayName => Column.CompletedWorkItems;
            public string Legend => "The number of work items that have been processed in ThreadPool (per single operation)";
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric) => Config.DisplayCompletedWorkItemCountWhenZero || metric.Value > 0;
        }

        private class LockContentionCountMetricDescriptor : MetricDescriptorConfigurationHandler<ThreadingDiagnoserConfig>, IMetricDescriptor
        {
            public static IMetricDescriptor Instance
                => MetricDescriptorSingletonBase<LockContentionCountMetricDescriptor>.Instance;

            public string Id => "LockContentionCount";
            public string DisplayName => Column.LockContentions;
            public string Legend => "The number of times there was contention upon trying to take a Monitor's lock (per single operation)";
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric) => Config.DisplayLockContentionWhenZero || metric.Value > 0;
        }
    }
}