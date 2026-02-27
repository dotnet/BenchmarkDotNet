using System.Diagnostics;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public class EnergyDiagnoser : IDiagnoser
    {
        private EnergyCounter[] _counters;

        private bool _validationFailed = false;

        private const string DiagnoserId = nameof(EnergyDiagnoser);

        public static readonly EnergyDiagnoser Default = new EnergyDiagnoser(new EnergyDiagnoserConfig());

        public EnergyDiagnoser(EnergyDiagnoserConfig config) => Config = config;

        public EnergyDiagnoserConfig Config { get; }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

        public IEnumerable<string> Ids => new[] { DiagnoserId };
        
        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();
        
        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();
        
        public void DisplayResults(ILogger logger) { }
        
        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            try
            {
                _counters = EnergyCounterDiscovery.Discover(Config.EnergyCountersSetup).ToArray();
                if (_counters.Length == 0)
                    throw new Exception("No RAPL counters found (or not enough rights)");

            }
            catch (Exception e)
            {
                _validationFailed = true;
                return [new ValidationError(false, e.Message)];
            }

            var errors = _counters.Select(ec => ec.TestRead()).Where(x => x.Item1 == false).Select(x => new ValidationError(false, x.Item2));
            _validationFailed = errors.Any();
            return errors;
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters _)
        {
            if (_validationFailed)
                return;

            if (signal == HostSignal.BeforeActualRun)
            {
                for (int i = 0; i < _counters.Length; i++)
                    _counters[i].FixStart();
            }
            else if (signal == HostSignal.AfterActualRun)
            {
                for (int i = 0; i < _counters.Length; i++)
                    _counters[i].FixFinish();
            }
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults diagnoserResults)
        {
            if (_validationFailed)
                yield break;

            long operations = diagnoserResults.Measurements.Where(m => m.IterationStage == IterationStage.Actual).Sum(m => m.Operations);
            Debug.Assert(operations > 0);

            int priority = 0;
            foreach (var energyCounter in _counters.OrderBy(c => c.Id))
            {
                long uj = energyCounter.GetValue();
                double avg_uj = operations > 0 && uj > 0 ? ((double)uj) / operations : 0.0;

                yield return new Metric(new EnergyMetricDescriptor(priority++, energyCounter.Name, energyCounter.Id), avg_uj);
            }
        }

        private class EnergyMetricDescriptor : IMetricDescriptor
        {
            public EnergyMetricDescriptor(int priority, string unitName, string id)
            {
                Id = id;
                DisplayName = $"EC {unitName}";
                Legend = $"Average energy consumption of unit {unitName}, uj/op";
                PriorityInCategory = priority;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Legend { get; }
            public string NumberFormat => "#,000.000 uj";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "uj";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory { get; }
            public bool GetIsAvailable(Metric metric) => metric.Value > 0;
        }
    }   
}
