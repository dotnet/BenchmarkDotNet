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
            logger.Write("<pre><code>");
            logger.WriteLine();
            var lastGroup = -1;
            foreach (var infoLine in EnvironmentInfo.GetCurrent().ToList("Host", true))
            {
                if (infoLine.Group != lastGroup) logger.WriteLineInfo(infoLine.ToString());
                else logger.WriteInfo(", " + infoLine);
                lastGroup = infoLine.Group;
            }
            logger.WriteLine();
            logger.Write("</code></pre>");
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
            logger.WriteLine();

            logger.WriteLine("<table>");

            logger.Write("<tr>");
            table.PrintLine(table.FullHeader, logger, "<th>", "</th>");
            logger.Write("</tr>");

            foreach (var line in table.FullContent)
            {
                logger.Write("<tr>");
                table.PrintLine(line, logger, "<td>", "</td>");
                logger.Write("</tr>");
            }

            logger.WriteLine("</table>");
        }
    }
}