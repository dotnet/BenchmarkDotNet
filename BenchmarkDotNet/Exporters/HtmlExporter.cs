using System;
using System.Linq;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class HtmlExporter : ExporterBase
    {
        protected override string FileExtension => "html";

        public static readonly IExporter Default = new HtmlExporter();

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            logger.WriteLine($"<pre><code>{EnvironmentHelper.GetCurrentInfo().ToFormattedString("Host")}</code></pre>");
            logger.WriteLine();

            PrintTable(summary.Table, logger);
        }

        private void PrintTable(SummaryTable table, ILogger logger)
        {
            if (table.FullContent.Length == 0)
            {
                logger.WriteLineError("<pre>There are no benchmarks found</pre>");
                return;
            }
            logger.Write("<pre><code>");
            table.PrintCommonColumns(logger);
            logger.WriteLine("</code></pre>");
            logger.NewLine();

            logger.WriteLine("<table>");
            var prefixLogger = new LoggerWithPrefix(logger, "\t");

            prefixLogger.Write("<tr>");
            table.PrintLine(table.FullHeader, prefixLogger, "<td>", "</td>");
            prefixLogger.WriteLine("</tr>");

            foreach (var line in table.FullContent)
            {
                prefixLogger.Write("<tr>");
                table.PrintLine(line, prefixLogger, "<td>", "</td>");
                prefixLogger.WriteLine("</tr>");
            }
            logger.WriteLine("</table>");

        }
    }
}