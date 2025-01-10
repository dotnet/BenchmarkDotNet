using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class CompositeExporter : IExporter
    {
        private readonly ImmutableArray<IExporter> exporters;
        private readonly ImmutableArray<IntegratedExport> integratedExports;

        public CompositeExporter(ImmutableArray<IExporter> exporters) => this.exporters = exporters;

        public CompositeExporter(ImmutableArray<IntegratedExport> integratedExports) => this.integratedExports = integratedExports;

        public string Name => nameof(CompositeExporter);

        public void ExportToLog(Summary summary, ILogger logger)
        {
            if (summary.GetColumns().IsNullOrEmpty())
                logger.WriteLineHint("You haven't configured any columns, your results will be empty");

            foreach (var exporter in exporters)
            {
                try
                {
                    exporter.ExportToLog(summary, logger);
                }
                catch (Exception e)
                {
                    logger.WriteLineError(e.ToString());
                }
            }
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            if (exporters != null && exporters.Any())
            {
                return exporters.SelectMany(exporter =>
                {
                    var files = new List<string>();
                    try
                    {
                        files.AddRange(exporter.ExportToFiles(summary, consoleLogger));
                    }
                    catch (Exception e)
                    {
                        consoleLogger.WriteLineError(e.ToString());
                    }
                    return files;
                });
            }
            else if (integratedExports != null && integratedExports.Any())
            {
                return integratedExports.SelectMany(exporter =>
                {
                    var files = new List<string>();
                    IEnumerable<string> dependencyFilePaths = new List<string>();
                    try
                    {
                        if (exporter.Dependencies.Any())
                        {
                            exporter.Dependencies.ForEach(d =>
                            {
                                files.AddRange(d.ExportToFiles(summary, consoleLogger));
                            });
                        }
                        if (exporter.WithExporter != null)
                        {
                            files.AddRange(exporter.WithExporter.ExportToFiles(summary, consoleLogger));
                        }
                        if (exporter.Exporter != null && exporter.Exporter is IntegratedExporterExtension exporterBase)
                        {
                            object? payload = null;
                            switch (exporter.ExportEnum)
                            {
                                case IntegratedExportEnum.HtmlExporterWithRPlotExporter:
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
            else
            {
                return Array.Empty<string>();
            }
        }
    }
}