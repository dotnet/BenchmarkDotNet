using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Properties;
using BenchmarkDotNet.Reports;
using ScottPlot;
using ScottPlot.Plottables;

namespace BenchmarkDotNet.Exporters.Plotting
{
    public class ScottPlotExporter : IExporter
    {
        public static readonly IExporter Default = new ScottPlotExporter();

        public string Name => nameof(ScottPlotExporter);

        public ScottPlotExporter(int width = 1920, int height = 1080)
        {
            this.Width = width;
            this.Height = height;
            this.IncludeBarPlot = true;
            this.RotateLabels = true;
        }

        /// <summary>
        /// Gets or sets the width of all plots in pixels.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the height of all plots in pixels.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether labels for Plot X-axis should be rotated.
        /// This allows for longer labels at the expense of chart height.
        /// </summary>
        public bool RotateLabels { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a bar plot for time-per-op
        /// measurement values should be exported.
        /// </summary>
        public bool IncludeBarPlot { get; set; }

        public void ExportToLog(Summary summary, ILogger logger)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            var title = summary.Title;
            var version = BenchmarkDotNetInfo.Instance.BrandTitle;
            var annotations = GetAnnotations(version);

            var (timeUnit, timeScale) = GetTimeUnit(summary.Reports.SelectMany(m => m.AllMeasurements));

            foreach (var benchmark in summary.Reports.GroupBy(r => r.BenchmarkCase.Descriptor.Type.Name))
            {
                var benchmarkName = benchmark.Key;

                // Get the measurement nanoseconds per op, divided by time scale, grouped by target and Job [param].
                var timeStats = from report in benchmark
                                let jobId = report.BenchmarkCase.DisplayInfo.Replace(report.BenchmarkCase.Descriptor.DisplayInfo + ": ", string.Empty)
                                from measurement in report.AllMeasurements
                                let measurementValue = measurement.Nanoseconds / measurement.Operations
                                group measurementValue / timeScale by (Target: report.BenchmarkCase.Descriptor.WorkloadMethodDisplayInfo, JobId: jobId) into g
                                select (g.Key.Target, g.Key.JobId, Mean: g.Average(), StdError: StandardError(g.ToList()));

                if (this.IncludeBarPlot)
                {
                    // <BenchmarkName>-barplot.png
                    yield return CreateBarPlot(
                        $"{title} - {benchmarkName}",
                        Path.Combine(summary.ResultsDirectoryPath, $"{title}-{benchmarkName}-barplot.png"),
                        $"Time ({timeUnit})",
                        "Target",
                        timeStats,
                        annotations);
                }

                /* TODO: Rest of the RPlotExporter plots.
                <BenchmarkName>-boxplot.png
                <BenchmarkName>-<MethodName>-density.png
                <BenchmarkName>-<MethodName>-facetTimeline.png
                <BenchmarkName>-<MethodName>-facetTimelineSmooth.png
                <BenchmarkName>-<MethodName>-<JobName>-timelineSmooth.png
                <BenchmarkName>-<MethodName>-<JobName>-timelineSmooth.png*/
            }
        }

        /// <summary>
        /// Calculate Standard Deviation.
        /// </summary>
        /// <param name="values">Values to calculate from.</param>
        /// <returns>Standard deviation of values.</returns>
        private static double StandardError(IReadOnlyList<double> values)
        {
            double average = values.Average();
            double sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
            double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / values.Count);
            return standardDeviation / Math.Sqrt(values.Count);
        }

        /// <summary>
        /// Gets the lowest appropriate time scale across all measurements.
        /// </summary>
        /// <param name="values">All measurements</param>
        /// <returns>A unit and scaling factor to convert from nanoseconds.</returns>
        private (string Unit, double ScaleFactor) GetTimeUnit(IEnumerable<Measurement> values)
        {
            var minValue = values.Select(m => m.Nanoseconds / m.Operations).DefaultIfEmpty(0d).Min();
            if (minValue > 1000000000d)
            {
                return ("sec", 1000000000d);
            }

            if (minValue > 1000000d)
            {
                return ("ms", 1000000d);
            }

            if (minValue > 1000d)
            {
                return ("us", 1000d);
            }

            return ("ns", 1d);
        }

        private string CreateBarPlot(string title, string fileName, string yLabel, string xLabel, IEnumerable<(string Target, string JobId, double Mean, double StdError)> data, IReadOnlyList<Annotation> annotations)
        {
            Plot plt = new Plot();
            plt.Title(title, 28);
            plt.YLabel(yLabel);
            plt.XLabel(xLabel);

            var palette = new ScottPlot.Palettes.Category10();

            var legendPalette = data.Select(d => d.JobId)
                .Distinct()
                .Select((jobId, index) => (jobId, index))
                .ToDictionary(t => t.jobId, t => palette.GetColor(t.index));

            plt.Legend.IsVisible = true;
            plt.Legend.Location = Alignment.UpperRight;
            var legend = data.Select(d => d.JobId)
                .Distinct()
                .Select((label, index) => new LegendItem()
                {
                    Label = label,
                    FillColor = legendPalette[label]
                })
                .ToList();

            plt.Legend.ManualItems.AddRange(legend);

            var jobCount = plt.Legend.ManualItems.Count;
            var ticks = data
                .Select((d, index) => new Tick(index, d.Target))
                .ToArray();
            plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
            plt.Axes.Bottom.MajorTickStyle.Length = 0;

            if (this.RotateLabels)
            {
                plt.Axes.Bottom.TickLabelStyle.Rotation = 45;
                plt.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleLeft;

                // determine the width of the largest tick label
                float largestLabelWidth = 0;
                foreach (Tick tick in ticks)
                {
                    PixelSize size = plt.Axes.Bottom.TickLabelStyle.Measure(tick.Label);
                    largestLabelWidth = Math.Max(largestLabelWidth, size.Width);
                }

                // ensure axis panels do not get smaller than the largest label
                plt.Axes.Bottom.MinimumSize = largestLabelWidth;
                plt.Axes.Right.MinimumSize = largestLabelWidth;
            }

            var bars = data
                .Select((d, index) => new Bar()
                {
                    Position = ticks[index].Position,
                    Value = d.Mean,
                    Error = d.StdError,
                    FillColor = legendPalette[d.JobId]
                });
            plt.Add.Bars(bars);

            // Tell the plot to autoscale with no padding beneath the bars
            plt.Axes.Margins(bottom: 0, right: .2);

            plt.PlottableList.AddRange(annotations);

            plt.SavePng(fileName, this.Width, this.Height);
            return Path.GetFullPath(fileName);
        }

        /// <summary>
        /// Provides a list of annotations to put over the data area.
        /// </summary>
        /// <param name="version">The version to be displayed.</param>
        /// <returns>A list of annotations for every plot.</returns>
        private IReadOnlyList<Annotation> GetAnnotations(string version)
        {
            var versionAnnotation = new Annotation()
            {
                Label =
                {
                    Text = version,
                    FontSize = 14,
                    ForeColor = new Color(0, 0, 0, 100)
                },
                OffsetY = 10,
                OffsetX = 20,
                Alignment = Alignment.LowerRight
            };


            return new[] { versionAnnotation };
        }
    }
}