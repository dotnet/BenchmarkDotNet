using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Properties;
using BenchmarkDotNet.Reports;
using ScottPlot;
using ScottPlot.Plottables;

namespace BenchmarkDotNet.Exporters.Plotting
{
    /// <summary>
    /// Provides plot exports as .png files.
    /// </summary>
    public class ScottPlotExporter : IExporter
    {
        /// <summary>
        /// Default instance of the exporter with default configuration.
        /// </summary>
        public static readonly IExporter Default = new ScottPlotExporter();

        /// <summary>
        /// Gets the name of the Exporter type.
        /// </summary>
        public string Name => nameof(ScottPlotExporter);

        /// <summary>
        /// Initializes a new instance of ScottPlotExporter.
        /// </summary>
        /// <param name="width">The width of all plots in pixels (optional). Defaults to 1920.</param>
        /// <param name="height">The height of all plots in pixels (optional). Defaults to 1080.</param>
        public ScottPlotExporter(int width = 1920, int height = 1080)
        {
            this.Width = width;
            this.Height = height;
            this.IncludeBarPlot = true;
            this.IncludeBoxPlot = true;
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
        /// Gets or sets the common font size for ticks, labels etc. (defaults to 14).
        /// </summary>
        public int FontSize { get; set; } = 14;

        /// <summary>
        /// Gets or sets the font size for the chart title. (defaults to 28).
        /// </summary>
        public int TitleFontSize { get; set; } = 28;

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

        /// <summary>
        /// Gets or sets a value indicating whether a box plot or whisker plot for time-per-op
        /// measurement values should be exported.
        /// </summary>
        public bool IncludeBoxPlot { get; set; }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="summary">This parameter is not used.</param>
        /// <param name="logger">This parameter is not used.</param>
        /// <exception cref="NotSupportedException"></exception>
        public void ExportToLog(Summary summary, ILogger logger)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Exports plots to .png file.
        /// </summary>
        /// <param name="summary">The summary to be exported.</param>
        /// <param name="consoleLogger">Logger to output to.</param>
        /// <returns>The file paths of every plot exported.</returns>
        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            var title = summary.Title;
            var version = BenchmarkDotNetInfo.Instance.BrandTitle;
            var annotations = GetAnnotations(version);

            var (timeUnit, timeScale) = GetTimeUnit(summary.Reports
                .SelectMany(m => m.AllMeasurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Result))));

            foreach (var benchmark in summary.Reports.GroupBy(r => r.BenchmarkCase.Descriptor.Type.Name))
            {
                var benchmarkName = benchmark.Key;

                // Get the measurement nanoseconds per op, divided by time scale, grouped by target and Job [param].
                var timeStats = from report in benchmark
                                let jobId = report.BenchmarkCase.DisplayInfo.Replace(report.BenchmarkCase.Descriptor.DisplayInfo + ": ", string.Empty)
                                from measurement in report.AllMeasurements
                                where measurement.Is(IterationMode.Workload, IterationStage.Result)
                                let measurementValue = measurement.Nanoseconds / measurement.Operations
                                group measurementValue / timeScale by (Target: report.BenchmarkCase.Descriptor.WorkloadMethodDisplayInfo, JobId: jobId) into g
                                select new ChartStats(g.Key.Target, g.Key.JobId, g.ToList());

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

                if (this.IncludeBoxPlot)
                {
                    // <BenchmarkName>-boxplot.png
                    yield return CreateBoxPlot(
                        $"{title} - {benchmarkName}",
                        Path.Combine(summary.ResultsDirectoryPath, $"{title}-{benchmarkName}-boxplot.png"),
                        $"Time ({timeUnit})",
                        "Target",
                        timeStats,
                        annotations);
                }

                /* TODO: Rest of the RPlotExporter plots.
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

        private string CreateBarPlot(string title, string fileName, string yLabel, string xLabel, IEnumerable<ChartStats> data, IReadOnlyList<Annotation> annotations)
        {
            Plot plt = new Plot();
            plt.Title(title, this.TitleFontSize);
            plt.YLabel(yLabel, this.FontSize);
            plt.XLabel(xLabel, this.FontSize);

            var palette = new ScottPlot.Palettes.Category10();

            var legendPalette = data.Select(d => d.JobId)
                .Distinct()
                .Select((jobId, index) => (jobId, index))
                .ToDictionary(t => t.jobId, t => palette.GetColor(t.index));

            plt.Legend.IsVisible = true;
            plt.Legend.Location = Alignment.UpperRight;
            plt.Legend.Font.Size = this.FontSize;
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

            plt.Axes.Left.TickLabelStyle.FontSize = this.FontSize;
            plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
            plt.Axes.Bottom.MajorTickStyle.Length = 0;
            plt.Axes.Bottom.TickLabelStyle.FontSize = this.FontSize;

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
                plt.Axes.Bottom.MinimumSize = largestLabelWidth * 2;
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

        private string CreateBoxPlot(string title, string fileName, string yLabel, string xLabel, IEnumerable<ChartStats> data, IReadOnlyList<Annotation> annotations)
        {
            Plot plt = new Plot();
            plt.Title(title, this.TitleFontSize);
            plt.YLabel(yLabel, this.FontSize);
            plt.XLabel(xLabel, this.FontSize);

            var palette = new ScottPlot.Palettes.Category10();

            var legendPalette = data.Select(d => d.JobId)
                .Distinct()
                .Select((jobId, index) => (jobId, index))
                .ToDictionary(t => t.jobId, t => palette.GetColor(t.index));

            plt.Legend.IsVisible = true;
            plt.Legend.Location = Alignment.UpperRight;
            plt.Legend.Font.Size = this.FontSize;
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

            plt.Axes.Left.TickLabelStyle.FontSize = this.FontSize;
            plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
            plt.Axes.Bottom.MajorTickStyle.Length = 0;
            plt.Axes.Bottom.TickLabelStyle.FontSize = this.FontSize;

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
                plt.Axes.Bottom.MinimumSize = largestLabelWidth * 2;
                plt.Axes.Right.MinimumSize = largestLabelWidth;
            }

            int globalIndex = 0;
            foreach (var (targetGroup, targetGroupIndex) in data.GroupBy(s => s.Target).Select((targetGroup, index) => (targetGroup, index)))
            {
                var boxes = targetGroup.Select(job => (job.JobId, Stats: job.CalculateBoxPlotStatistics())).Select((j, jobIndex) => new Box()
                    {
                        Position = ticks[globalIndex++].Position,
                        Fill = new FillStyle() { Color = legendPalette[j.JobId] },
                        Stroke = new LineStyle() { Color = Colors.Black },
                        BoxMin = j.Stats.Q1,
                        BoxMax = j.Stats.Q3,
                        WhiskerMin = j.Stats.Min,
                        WhiskerMax = j.Stats.Max,
                        BoxMiddle = j.Stats.Median
                    })
                    .ToList();
                plt.Add.Boxes(boxes);
            }

            // Tell the plot to autoscale with a small padding below the boxes.
            plt.Axes.Margins(bottom: 0.05, right: .2);

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

        private class ChartStats
        {
            public ChartStats(string Target, string JobId, IReadOnlyList<double> Values)
            {
                this.Target = Target;
                this.JobId = JobId;
                this.Values = Values;
            }

            public string Target { get; }

            public string JobId { get; }

            public IReadOnlyList<double> Values { get; }

            public double Min => this.Values.DefaultIfEmpty(0d).Min();

            public double Max => this.Values.DefaultIfEmpty(0d).Max();

            public double Mean => this.Values.DefaultIfEmpty(0d).Average();

            public double StdError => StandardError(this.Values);


            private static (int MidPoint, double Median) CalculateMedian(ReadOnlySpan<double> values)
            {
                int n = values.Length;
                var midPoint = n / 2;

                // Check if count is even, if so use average of the two middle values,
                // otherwise take the middle value.
                var median = n % 2 == 0 ? (values[midPoint - 1] + values[midPoint]) / 2d : values[midPoint];
                return (midPoint, median);
            }

            /// <summary>
            /// Calculate the mid points.
            /// </summary>
            /// <returns></returns>
            public (double Min, double Q1, double Median, double Q3, double Max, double[] Outliers) CalculateBoxPlotStatistics()
            {
                var values = this.Values.ToArray();
                Array.Sort(values);
                var s = values.AsSpan();
                var (midPoint, median) = CalculateMedian(s);

                var (q1Index, q1) = midPoint > 0 ? CalculateMedian(s.Slice(0, midPoint)) : (midPoint, median);
                var (q3Index, q3) = midPoint + 1 < s.Length ? CalculateMedian(s.Slice(midPoint + 1)) : (midPoint, median);
                var iqr = q3 - q1;
                var lowerFence = q1 - 1.5d * iqr;
                var upperFence = q3 + 1.5d * iqr;
                var outliers = values.Where(v => v < lowerFence || v > upperFence).ToArray();
                var nonOutliers = values.Where(v => v >= lowerFence && v <= upperFence).ToArray();
                return (
                    nonOutliers.FirstOrDefault(),
                    q1,
                    median,
                    q3,
                    nonOutliers.LastOrDefault(),
                    outliers
                );
            }
        }
    }
}