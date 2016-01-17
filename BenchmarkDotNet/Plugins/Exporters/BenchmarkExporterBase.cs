using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.ResultExtenders;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public abstract class BenchmarkExporterBase : IBenchmarkExporter
    {
        public virtual string FileExtension => "txt";
        public virtual string FileNameSuffix => string.Empty;
        public virtual string FileCaption => "report";
        
        public abstract string Name { get; }
        public abstract string Description { get; }

        public abstract void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger, IEnumerable<IBenchmarkResultExtender> resultExtenders = null);

        public IEnumerable<string> ExportToFile(IList<BenchmarkReport> reports, string fileNamePrefix, IEnumerable<IBenchmarkResultExtender> resultExtenders = null)
        {
            var fileName = $"{fileNamePrefix}-{FileCaption}{FileNameSuffix}.{FileExtension}";
            using (var stream = new StreamWriter(fileName))
                Export(reports, new BenchmarkStreamLogger(stream), resultExtenders);
            yield return fileName;
        }        
    }
}