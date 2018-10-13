using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Reports;

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
            var workload = entire.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).GetStatistics();
            
            var cpuResolution = currentFrequency.Value.ToResolution();

            var zeroMeasurement = overheadMeasurements.Any()
                ? ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(workload.WithoutOutliers(), overheadMeasurements.GetStatistics().WithoutOutliers())
                : ZeroMeasurementHelper.CheckZeroMeasurementOneSample(workload.WithoutOutliers(), cpuResolution.Nanoseconds / 2);
            
            if (zeroMeasurement)
                yield return CreateWarning($"It seems that result {entire.Where(m => m.Is(IterationMode.Workload, IterationStage.Result)).GetStatistics().Mean:0.####} is too small to be valid with CPU resolution {cpuResolution}", report);
        }
    }
}