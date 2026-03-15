using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class HtmlExporter : ExporterBase
    {
        private const string CssDefinition = @"
<style type=""text/css"">
	table { border-collapse: collapse; display: block; width: 100%; overflow: auto; }
	td, th { padding: 6px 13px; border: 1px solid #ddd; text-align: right; }
	tr { background-color: #fff; border-top: 1px solid #ccc; }
	tr:nth-child(even) { background: #f8f8f8; }
</style>";

        protected override string FileExtension => "html";

        public static readonly IExporter Default = new HtmlExporter();

        protected override async ValueTask ExportAsync(Summary summary, StreamOrLoggerWriter writer, CancellationToken cancellationToken)
        {
            await writer.WriteLineAsync("<!DOCTYPE html>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("<html lang='en'>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("<head>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("<meta charset='utf-8' />", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("<title>" + summary.Title + "</title>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync(CssDefinition, cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("</head>", cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync("<body>", cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync("<pre><code>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
            foreach (string infoLine in summary.HostEnvironmentInfo.ToFormattedString())
            {
                await writer.WriteLineAsync(Escape(infoLine), cancellationToken).ConfigureAwait(false);
            }
            await writer.WriteLineAsync(Escape(summary.AllRuntimes), cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync("</code></pre>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);

            await PrintTableAsync(summary.Table, writer, cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync("</body>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("</html>", cancellationToken).ConfigureAwait(false);
        }

        private static async ValueTask PrintTableAsync(SummaryTable table, StreamOrLoggerWriter writer, CancellationToken cancellationToken)
        {
            if (table.FullContent.Length == 0)
            {
                await writer.WriteLineAsync("<pre>There are no benchmarks found</pre>", LogKind.Error, cancellationToken).ConfigureAwait(false);
                return;
            }

            await writer.WriteAsync("<pre><code>", cancellationToken).ConfigureAwait(false);
            await table.PrintCommonColumnsAsync(writer, cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("</code></pre>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync("<table>", cancellationToken).ConfigureAwait(false);

            await writer.WriteAsync("<thead>", cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync("<tr>", cancellationToken).ConfigureAwait(false);
            await table.PrintLineAsync(table.FullHeader, writer, "<th>", "</th>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("</tr>", cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync("</thead>", cancellationToken).ConfigureAwait(false);

            await writer.WriteAsync("<tbody>", cancellationToken).ConfigureAwait(false);
            foreach (var line in table.FullContent)
            {
                await writer.WriteAsync("<tr>", cancellationToken).ConfigureAwait(false);
                await PrintLineAsync(table, line, writer, "<td>", "</td>", cancellationToken).ConfigureAwait(false);
                await writer.WriteAsync("</tr>", cancellationToken).ConfigureAwait(false);
            }
            await writer.WriteAsync("</tbody>", cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync("</table>", cancellationToken).ConfigureAwait(false);
        }

        private static async ValueTask PrintLineAsync(SummaryTable table, string[] line, StreamOrLoggerWriter writer, string leftDel, string rightDel, CancellationToken cancellationToken)
        {
            for (int columnIndex = 0; columnIndex < table.ColumnCount; columnIndex++)
            {
                if (table.Columns[columnIndex].NeedToShow)
                {
                    await writer.WriteAsync(leftDel + line[columnIndex].HtmlEncode() + rightDel, LogKind.Statistic, cancellationToken).ConfigureAwait(false);
                }
            }

            await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
        }

        private static string Escape(string text)
        {
            return text.Replace("\u03BC", "&mu;");
        }
    }
}
