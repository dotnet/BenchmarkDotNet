using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BenchmarkDotNet.Exporters
{
    public abstract class IntegratedExporterExtension : ExporterBase
    {
        public abstract void IntegratedExportToLog(Summary summary, ILogger logger, object moreData);

        public IEnumerable<string> IntegratedExportToFiles(Summary summary, ILogger consoleLogger, object moreData = null)
        {
            string fileName = GetFileName(summary);
            string filePath = GetArtifactFullName(summary);
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException)
                {
                    string uniqueString = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    string alternativeFilePath = $"{Path.Combine(summary.ResultsDirectoryPath, fileName)}-{FileCaption}{FileNameSuffix}-{uniqueString}.{FileExtension}";
                    consoleLogger.WriteLineError($"Could not overwrite file {filePath}. Exporting to {alternativeFilePath}");
                    filePath = alternativeFilePath;
                }
            }

            using (var stream = new StreamWriter(filePath, append: false))
            {
                using (var streamLogger = new StreamLogger(stream))
                {
                    IntegratedExportToLog(summary, streamLogger, moreData);
                }
            }

            return new[] { filePath };
        }

    }
}
