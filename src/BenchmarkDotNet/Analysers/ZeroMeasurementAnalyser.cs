using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Analysers
{
    public class ZeroMeasurementAnalyser : AnalyserBase
    {
        public override string Id => "ZeroMeasurement";

        public static readonly IAnalyser Default = new ZeroMeasurementAnalyser();

        private static readonly TimeInterval FallbackCpuResolutionValue = TimeInterval.FromNanoseconds(0.2d);

        private ZeroMeasurementAnalyser() { }

        protected override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            var currentFrequency = summary.HostEnvironmentInfo.CpuInfo.Value.MaxFrequency;
            if (!currentFrequency.HasValue || currentFrequency <= 0)
                currentFrequency = FallbackCpuResolutionValue.ToFrequency();

            var entire = report.AllMeasurements;
            var overheadMeasurements = entire.Where(m => m.Is(IterationMode.Overhead, IterationStage.Actual)).ToArray();
            var workloadMeasurements = entire.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).ToArray();
            if (workloadMeasurements.IsEmpty())
                yield break;
            var workload = workloadMeasurements.GetStatistics();

            var threshold = currentFrequency.Value.ToResolution().Nanoseconds / 2;

            var zeroMeasurement = overheadMeasurements.Any()
                ? ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(workload.WithoutOutliers(), overheadMeasurements.GetStatistics().WithoutOutliers())
                : ZeroMeasurementHelper.CheckZeroMeasurementOneSample(workload.WithoutOutliers(), threshold);

            if (zeroMeasurement)
                yield return CreateWarning("The method duration is indistinguishable from the empty method duration",
                                           report, false);
        }
    }
}