using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers;

internal sealed class NullDiagnoser : IDiagnoser
{
    public static IDiagnoser Instance { get; } = new NullDiagnoser();

    private NullDiagnoser() { }

    public IEnumerable<string> Ids => [];
    public IEnumerable<IExporter> Exporters => [];
    public IEnumerable<IAnalyser> Analysers => [];
    public void DisplayResults(ILogger logger) { }
    public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.None;
    public ValueTask HandleAsync(HostSignal signal, DiagnoserActionParameters parameters, CancellationToken cancellationToken) => new();
    public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];
    public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters) => AsyncEnumerable.Empty<ValidationError>();
}
