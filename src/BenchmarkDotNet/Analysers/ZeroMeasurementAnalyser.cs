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
            var currentFrequency = summary.HostEnvironmentInfo.Cpu.Value.MaxFrequency();
            if (!currentFrequency.HasValue || currentFrequency <= 0)
                currentFrequency = FallbackCpuResolutionValue.ToFrequency();

            var entire = report.AllMeasurements;
            var overheadMeasurements = entire.Where(m => m.Is(IterationMode.Overhead, IterationStage.Actual)).ToArray();
            var workloadMeasurements = entire.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).ToArray();
            if (workloadMeasurements.IsEmpty())
                yield break;

            var workloadSample = workloadMeasurements.GetStatistics().Sample;
            var threshold = currentFrequency.Value.ToResolution().Nanoseconds / 2;

            var zeroMeasurement = overheadMeasurements.Any()
                ? ZeroMeasurementHelper.AreIndistinguishable(workloadSample, overheadMeasurements.GetStatistics().Sample)
                : ZeroMeasurementHelper.IsNegligible(workloadSample, threshold);

            if (zeroMeasurement)
                yield return CreateWarning("The method duration is indistinguishable from the empty method duration",
                    report, false);
        }
    }
}