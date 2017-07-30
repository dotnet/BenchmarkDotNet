using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.InProcess;

namespace BenchmarkDotNet.Analysers
{
    public class EnvironmentAnalyser : AnalyserBase
    {
        public override string Id => "Environment";
        public static readonly IAnalyser Default = new EnvironmentAnalyser();

        private EnvironmentAnalyser()
        {
        }

        public override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            if (report.Benchmark.Target.Type.GetTypeInfo().Assembly.IsJitOptimizationDisabled().IsTrue())
                yield return CreateWarning("Benchmark was built without optimization enabled (most probably a DEBUG configuration). Please, build it in RELEASE.", report);
        }

        public override IEnumerable<Conclusion> AnalyseSummary(Summary summary)
        {
            if (summary.HostEnvironmentInfo.HasAttachedDebugger)
                yield return CreateWarning("Benchmark was executed with attached debugger");

            var antivirusProducts = summary.HostEnvironmentInfo.AntivirusProducts.Value;
            if (antivirusProducts.Any())
                yield return CreateWarning("Found antivirus products: " + $"{string.Join(", ", antivirusProducts)}. " +
                                           $"In case of any problems or hang, use {nameof(InProcessToolchain)} to avoid new process creation.");
        }
    }
}