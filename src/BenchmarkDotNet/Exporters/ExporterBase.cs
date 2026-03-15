using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
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

        // This will execute synchronously (because ILogger doesn't support async),
        // but it's needed to re-use the logic for both file and logger export to avoid code duplication on every implementation.
        internal async ValueTask ExportToLogAsync(Summary summary, ILogger logger, CancellationToken cancellationToken)
        {
            var writer = new LoggerWriter(logger);
            await ExportAsync(summary, writer, cancellationToken).ConfigureAwait(false);
        }

        protected abstract ValueTask ExportAsync(Summary summary, StreamOrLoggerWriter writer, CancellationToken cancellationToken);

        public async ValueTask ExportAsync(Summary summary, ILogger logger, CancellationToken cancellationToken)
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
                    logger.WriteLineError($"Could not overwrite file {filePath}. Exporting to {alternativeFilePath}");
                    filePath = alternativeFilePath;
                }
            }

            using var fileStream = File.Create(filePath);
            using var writer = new CancelableStreamWriter(fileStream);
            await ExportAsync(summary, writer, cancellationToken).ConfigureAwait(false);

            logger.WriteLineInfo($"  {filePath.GetBaseName(Directory.GetCurrentDirectory())}");
        }

        public string GetArtifactFullName(Summary summary)
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