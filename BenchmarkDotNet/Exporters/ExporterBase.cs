using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public abstract class ExporterBase : IExporter
    {
        protected virtual string FileExtension => "txt";
        protected virtual string FileNameSuffix => string.Empty;
        protected virtual string FileCaption => "report";

        public abstract void ExportToLog(Summary summary, ILogger logger);

        public IEnumerable<string> ExportToFiles(Summary summary)
        {
            var fileName = $"{Path.Combine(summary.CurrentDirectory, summary.Title)}-{FileCaption}{FileNameSuffix}.{FileExtension}";
            using (var stream = new StreamWriter(fileName))
                ExportToLog(summary, new StreamLogger(stream));
            yield return fileName;
        }
    }
}