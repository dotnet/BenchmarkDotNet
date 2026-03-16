using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Tests.Helpers;

internal static class ExporterTestHelper
{
    internal static async ValueTask ExportToLogAsync(this ExporterBase exporter, Summary summary, ILogger logger, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        using (var writer = new CancelableStreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true))
        {
            await exporter.ExportAsync(summary, writer, cancellationToken).ConfigureAwait(false);
        }

        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? line;
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
        {
            logger.WriteLine(line);
        }
    }
}
