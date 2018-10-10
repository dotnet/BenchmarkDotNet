using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers 
{
    public class ZeroMeasurementAnalyser : AnalyserBase 
    {
        public override string Id => "ZeroMeasurement";
        
        public static readonly IAnalyser Default = new ZeroMeasurementAnalyser();

        private const double FallbackCPUResolutionValue = 0.2d;
        
        private ZeroMeasurementAnalyser() { }

        protected override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            var currentFrequency = summary.HostEnvironmentInfo.CpuInfo.Value.MaxFrequency;
            if (!currentFrequency.HasValue || currentFrequency <= 0) yield break;
            
            var result = report.AllMeasurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Result)).ToArray();
            var cpuResolution = currentFrequency.Value.ToResolution();
            var stats = result.GetStatistics();
            
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurement(stats.WithoutOutliers(), cpuResolution.Nanoseconds / 2);
            
            if (zeroMeasurement) yield return CreateWarning($"It seems that result {stats.Mean:0.####} is too small to be valid with CPU resolution {cpuResolution}", report);
        }
    }
}