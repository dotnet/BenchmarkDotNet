using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

#nullable enable

namespace BenchmarkDotNet.Diagnosers
{
    public class ExceptionDiagnoser(ExceptionDiagnoserConfig config) : IInProcessDiagnoser
    {
        public static readonly ExceptionDiagnoser Default = new(new ExceptionDiagnoserConfig(displayExceptionsIfZeroValue: true));

        private readonly Dictionary<BenchmarkCase, long> results = [];

        public ExceptionDiagnoserConfig Config { get; } = config;

        public IEnumerable<string> Ids => [nameof(ExceptionDiagnoser)];

        public IEnumerable<IExporter> Exporters => [];

        public IEnumerable<IAnalyser> Analysers => [];

        public void DisplayResults(ILogger logger) { }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.ExtraIteration;

        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults diagnoserResults)
        {
            if (results.TryGetValue(diagnoserResults.BenchmarkCase, out var exceptionsCount))
            {
                double totalOperations = diagnoserResults.Measurements.First(m => m.IterationStage == IterationStage.Extra).Operations;
                yield return new Metric(new ExceptionsFrequencyMetricDescriptor(Config), exceptionsCount / totalOperations);
            }
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => [];

        void IInProcessDiagnoser.DeserializeResults(BenchmarkCase benchmarkCase, string serializedResults)
            => results.Add(benchmarkCase, long.Parse(serializedResults));

        InProcessDiagnoserHandlerData IInProcessDiagnoser.GetHandlerData(BenchmarkCase benchmarkCase)
            => new(typeof(ExceptionDiagnoserInProcessHandler), null);

        internal class ExceptionsFrequencyMetricDescriptor(ExceptionDiagnoserConfig config) : IMetricDescriptor
        {
            public string Id => "ExceptionFrequency";
            public string DisplayName => Column.Exceptions;
            public string Legend => "Exceptions thrown per single operation";
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric)
                => config?.DisplayExceptionsIfZeroValue == true || metric.Value > 0;
        }
    }

    [UsedImplicitly]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ExceptionDiagnoserInProcessHandler : IInProcessDiagnoserHandler
    {
        private long exceptionsCount;

        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        void IInProcessDiagnoserHandler.Handle(BenchmarkSignal signal, InProcessDiagnoserActionArgs args)
        {
            switch (signal)
            {
                case BenchmarkSignal.BeforeExtraIteration:
                    AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
                    break;
                case BenchmarkSignal.AfterExtraIteration:
                    AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
                    break;
            }
        }

        void IInProcessDiagnoserHandler.Initialize(string? serializedConfig) { }

        string IInProcessDiagnoserHandler.SerializeResults()
            => exceptionsCount.ToString();

        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        private void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            Interlocked.Increment(ref exceptionsCount);
        }
    }
}