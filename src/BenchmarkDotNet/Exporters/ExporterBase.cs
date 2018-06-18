using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public abstract class ExporterBase : IExporter
    {
        public string Name => $"{GetType().Name}{FileNameSuffix}";
        public Encoding Encoding { get; }

        protected virtual string FileExtension => "txt";
        protected virtual string FileNameSuffix => string.Empty;
        protected virtual string FileCaption => "report";

        public abstract void ExportToLog(Summary summary, ILogger logger);

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            string fileName = GetFileName(summary);
            string filePath = GetAtrifactFullName(summary);
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

        internal string GetAtrifactFullName(Summary summary)
        {
            string fileName = GetFileName(summary);
            return $"{Path.Combine(summary.ResultsDirectoryPath, fileName)}-{FileCaption}{FileNameSuffix}.{FileExtension}";
        }

        private static string GetFileName(Summary summary)
        {
            // we can't use simple name here, because user might be running benchmarks for a library,  which defines few types with the same name
            // and reports the results per type, so every summary is going to contain just single benchmark
            // and we can't tell here if there is a name conflict or not
            var targets = summary.BenchmarksCases.Select(b => b.Descriptor.Type).Distinct().ToArray();

            if (targets.Length == 1)
                return FolderNameHelper.ToFolderName(targets.Single());

            return summary.Title;
        }
    }
}