using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters;

public interface IExporter
{
    string Name { get; }

    ValueTask ExportAsync(Summary summary, ILogger logger, CancellationToken cancellationToken);
}