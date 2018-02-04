using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Extensions;
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
            var benchmarkTypes = summary.Benchmarks.Select(b => b.Target.Type).Distinct().Select(type => GetTypeName(type)).ToArray();

            return benchmarkTypes.Length == 1 ? benchmarkTypes[0] : summary.Title;
        }

        // we can't simply use type.FullName, because for generics it's tooo long
        // example: typeof(List<int>).FullName => "System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"
        private static string GetTypeName(Type type)
            => new StringBuilder(type.GetCorrectTypeName())
                .Replace('<', '_')
                .Replace('>', '_')
                .ToString();
    }
}