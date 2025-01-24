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

            foreach (var exporter in exporters)
            {
                try
                {

                    foreach (var dependencyExporter in exporter?.Dependencies ?? new List<IExporter>())
                    {
                        dependencyExporter.ExportToLog(summary, logger);
                    }
                    exporter.WithExporter.ExportToLog(summary, logger);
                    exporter.Exporter.ExportToLog(summary, logger);
                }
                catch (Exception e)
                {
                    logger.WriteLineError(e.ToString());
                }
            }
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            return exporters.SelectMany(exporter =>
            {
                var files = new List<string>();
                try
                {
                    var dependencyFiles = (exporter?.Dependencies ?? new List<IExporter>()).SelectMany(dependencyExporter => dependencyExporter.ExportToFiles(summary, consoleLogger));
                    files.AddRange(dependencyFiles);
                    foreach (var file in exporter.WithExporter.ExportToFiles(summary, consoleLogger))
                    {
                        files.Add(file);
                    }
                    if (exporter.Exporter != null && exporter.Exporter is IntegratedExporterExtension exporterBase)
                    {
                        object? payload = null;
                        switch (exporter.ExporterType)
                        {
                            case IntegratedExportType.HtmlExporterWithRPlotExporter:
                                if (exporter.WithExporter is RPlotExporter rPlotExporter)
                                {
                                    payload = rPlotExporter.GetExpectedPngPaths(summary, consoleLogger);
                                }
                                break;
                        }
                        files.AddRange(exporterBase.IntegratedExportToFiles(summary, consoleLogger, payload));
                    }
                }
                catch (Exception e)
                {
                    consoleLogger.WriteLineError(e.ToString());
                }
                return files;
            });
        }
    }
}