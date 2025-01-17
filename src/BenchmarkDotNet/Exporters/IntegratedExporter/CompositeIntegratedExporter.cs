using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace BenchmarkDotNet.Exporters.IntegratedExporter
{
    internal class CompositeIntegratedExporter : IExporter
    {
        private readonly ImmutableArray<IntegratedExporterData> exporters;

        public CompositeIntegratedExporter(ImmutableArray<IntegratedExporterData> integratedExporters) => this.exporters = integratedExporters;

        public string Name => nameof(CompositeExporter);

        public void ExportToLog(Summary summary, ILogger logger)
        {
            if (summary.GetColumns().IsNullOrEmpty())
                logger.WriteLineHint("You haven't configured any columns, your results will be empty");
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            //else if (integratedExports != null && integratedExports.Any())
            //{
            //    return integratedExports.SelectMany(exporter =>
            //    {
            //        var files = new List<string>();
            //        IEnumerable<string> dependencyFilePaths = new List<string>();
            //        try
            //        {
            //            if (exporter.Dependencies.Any())
            //            {
            //                exporter.Dependencies.ForEach(d =>
            //                {
            //                    files.AddRange(d.ExportToFiles(summary, consoleLogger));
            //                });
            //            }
            //            if (exporter.WithExporter != null)
            //            {
            //                files.AddRange(exporter.WithExporter.ExportToFiles(summary, consoleLogger));
            //            }
            //            if (exporter.Exporter != null && exporter.Exporter is IntegratedExporterExtension exporterBase)
            //            {
            //                object? payload = null;
            //                switch (exporter.ExportEnum)
            //                {
            //                    case IntegratedExporterType.HtmlExporterWithRPlotExporter:
            //                        if (exporter.WithExporter is RPlotExporter rPlotExporter)
            //                        {
            //                            payload = rPlotExporter.GetExpectedPngPaths(summary, consoleLogger);
            //                        }
            //                        break;
            //                }
            //                files.AddRange(exporterBase.IntegratedExportToFiles(summary, consoleLogger, payload));
            //            }
            //        }
            //        catch (Exception e)
            //        {
            //            consoleLogger.WriteLineError(e.ToString());
            //        }
            //        return files;
            //    });
            //}
            return Array.Empty<string>();
        }
    }
}
