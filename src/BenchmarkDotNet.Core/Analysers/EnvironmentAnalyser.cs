using System.Collections.Generic;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public class EnvironmentAnalyser : AnalyserBase
    {
        public override string Id => "Environment";
        public static readonly IAnalyser Default = new EnvironmentAnalyser();

        private EnvironmentAnalyser()
        {
        }

        public override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report)
        {
            if (report.Benchmark.Target.Type.GetTypeInfo().Assembly.IsDebug().IsTrue())
                yield return CreateWarning("Benchmark was built in DEBUG configuration. Please, build it in RELEASE.", report);
        }

        public override IEnumerable<Conclusion> AnalyseSummary(Summary summary)
        {
            if (summary.HostEnvironmentInfo.HasAttachedDebugger)
                yield return CreateWarning("Benchmark was executed with attached debugger");
        }
    }
}