using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.IntegrationTests;

/// <summary>
/// Exports nothing, for configs that don't need the results persisted but shouldn't fail validation for having no exporter.
/// </summary>
internal sealed class NullExporter : IExporter
{
    public static readonly NullExporter Instance = new();

    private NullExporter() { }

    public string Name => nameof(NullExporter);

    public ValueTask ExportAsync(Summary summary, ILogger logger, CancellationToken cancellationToken) => default;
}
