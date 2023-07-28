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

namespace BenchmarkDotNet.Diagnosers
{
    public class ThreadingDiagnoser : IDiagnoser
    {
        public static readonly ThreadingDiagnoser Default = new ThreadingDiagnoser();

        private ThreadingDiagnoser() { }

        public IEnumerable<string> Ids => new[] { nameof(ThreadingDiagnoser) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public void DisplayResults(ILogger logger) { }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            yield return new Metric(CompletedWorkItemCountMetricDescriptor.Instance, results.ThreadingStats.CompletedWorkItemCount / (double)results.ThreadingStats.TotalOperations);
            yield return new Metric(LockContentionCountMetricDescriptor.Instance, results.ThreadingStats.LockContentionCount / (double)results.ThreadingStats.TotalOperations);
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            foreach (var benchmark in validationParameters.Benchmarks)
            {
                var runtime = benchmark.Job.ResolveValue(EnvironmentMode.RuntimeCharacteristic, EnvironmentResolver.Instance);

                if (runtime.RuntimeMoniker < RuntimeMoniker.NetCoreApp30)
                {
                    yield return new ValidationError(true, $"{nameof(ThreadingDiagnoser)} supports only .NET Core 3.0+", benchmark);
                }
            }
        }

        private class CompletedWorkItemCountMetricDescriptor : IMetricDescriptor
        {
            internal static readonly IMetricDescriptor Instance = new CompletedWorkItemCountMetricDescriptor();

            public string Id => "CompletedWorkItemCount";
            public string DisplayName => Column.CompletedWorkItems;
            public string Legend => "The number of work items that have been processed in ThreadPool (per single operation)";
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric) => true;
        }

        private class LockContentionCountMetricDescriptor : IMetricDescriptor
        {
            internal static readonly IMetricDescriptor Instance = new LockContentionCountMetricDescriptor();

            public string Id => "LockContentionCount";
            public string DisplayName => Column.LockContentions;
            public string Legend => "The number of times there was contention upon trying to take a Monitor's lock (per single operation)";
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric) => true;
        }
    }
}