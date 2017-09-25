using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var fileName = GetFileName(summary);
            var filePath = $"{Path.Combine(summary.ResultsDirectoryPath, fileName)}-{FileCaption}{FileNameSuffix}.{FileExtension}";
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException)
                {
                    var uniqueString = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    var alternativeFilePath = $"{Path.Combine(summary.ResultsDirectoryPath, fileName)}-{FileCaption}{FileNameSuffix}-{uniqueString}.{FileExtension}";
                    consoleLogger.WriteLineError($"Could not overwrite file {filePath}. Exporting to {alternativeFilePath}");
                    filePath = alternativeFilePath;
                }
            }

            using (var stream = Portability.StreamWriter.FromPath(filePath))
            {
                ExportToLog(summary, new StreamLogger(stream));
            }

            return new[] { filePath };
        }

        private static string GetFileName(Summary summary)
        {
            string fileName;

            var benchmarkTypes = summary.Benchmarks.Select(b => b.Target.Type.FullName).Distinct().ToArray();
            if (benchmarkTypes.Length == 1)
            {
                fileName = benchmarkTypes[0];
            }
            else
            {
                fileName = summary.Title;
            }

            return fileName;
        }
    }
}