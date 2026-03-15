using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters;

public sealed class CompositeExporter(ImmutableArray<IExporter> exporters) : IExporter
{
    public string Name => nameof(CompositeExporter);

    public async ValueTask ExportAsync(Summary summary, ILogger logger, CancellationToken cancellationToken)
    {
        if (summary.GetColumns().IsNullOrEmpty())
            logger.WriteLineHint("You haven't configured any columns, your results will be empty");

        foreach (var exporter in exporters)
        {
            try
            {
                await exporter.ExportAsync(summary, logger, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.WriteLineError(e.ToString());
            }
        }
    }
}