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

        public abstract ValueTask ExportAsync(Summary summary, CancelableStreamWriter writer, CancellationToken cancellationToken);

        public async ValueTask ExportAsync(Summary summary, ILogger logger, CancellationToken cancellationToken)
        {
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
                    string alternativeFilePath = $"{Path.Combine(summary.ResultsDirectoryPath, summary.Title)}-{FileCaption}{FileNameSuffix}-{uniqueString}.{FileExtension}";
                    logger.WriteLineError($"Could not overwrite file {filePath}. Exporting to {alternativeFilePath}");
                    filePath = alternativeFilePath;
                }
            }

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
            using var writer = new CancelableStreamWriter(fileStream);
            await ExportAsync(summary, writer, cancellationToken).ConfigureAwait(false);

            logger.WriteLineInfo($"  {filePath.GetBaseName(Directory.GetCurrentDirectory())}");
        }

        /// <summary>
        /// the exported files are named after the title of the summary, which is guaranteed to be unique
        /// within a single run and valid for a path (see <see cref="Helpers.TitleHelper"/>)
        /// </summary>
        public string GetArtifactFullName(Summary summary)
            => $"{Path.Combine(summary.ResultsDirectoryPath, summary.Title)}-{FileCaption}{FileNameSuffix}.{FileExtension}";
    }
}