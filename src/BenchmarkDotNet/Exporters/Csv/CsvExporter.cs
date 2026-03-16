using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Exporters.Csv
{
    public class CsvExporter : ExporterBase
    {
        private readonly CsvSeparator separator;
        private readonly SummaryStyle? style;
        protected override string FileExtension => "csv";

        public static readonly IExporter Default = new CsvExporter(CsvSeparator.CurrentCulture);

        [PublicAPI]
        public CsvExporter(CsvSeparator separator, SummaryStyle? style = null)
        {
            this.separator = separator;
            this.style = style;
        }

        public override async ValueTask ExportAsync(Summary summary, CancelableStreamWriter writer, CancellationToken cancellationToken)
        {
            string realSeparator = separator.ToRealSeparator();
            var exportStyle = (style ?? summary.Style).WithZeroMetricValuesInContent();
            foreach (var line in summary.GetTable(exportStyle).FullContentWithHeader)
            {
                for (int i = 0; i < line.Length;)
                {
                    await writer.WriteAsync(CsvHelper.Escape(line[i], realSeparator), cancellationToken).ConfigureAwait(false);

                    if (++i < line.Length)
                    {
                        await writer.WriteAsync(realSeparator, cancellationToken).ConfigureAwait(false);
                    }
                }

                await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
