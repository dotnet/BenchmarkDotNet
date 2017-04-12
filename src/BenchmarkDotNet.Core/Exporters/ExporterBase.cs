using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public abstract class ExporterBase : IExporter
    {
        public string Name => $"{GetType().Name}{FileNameSuffix}";

        protected virtual string FileExtension => "txt";
        protected virtual string FileNameSuffix => string.Empty;
        protected virtual string FileCaption => "report";

        public abstract void ExportToLog(Summary summary, ILogger logger);

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            var filePath = $"{Path.Combine(summary.ResultsDirectoryPath, summary.Title)}-{FileCaption}{FileNameSuffix}.{FileExtension}";
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException)
                {
                    var uniqueString = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    var alternativeFilePath = $"{Path.Combine(summary.ResultsDirectoryPath, summary.Title)}-{FileCaption}{FileNameSuffix}-{uniqueString}.{FileExtension}";
                    consoleLogger.WriteLineError($"Could not overwrite file {filePath}. Exporting to {alternativeFilePath}");
                    filePath = alternativeFilePath;
                }
            }

            using (var stream = Portability.StreamWriter.FromPath(filePath))
            {
                ExportToLog(summary, new StreamLogger(stream));
            }

            return new [] { filePath };
        }
    }
}