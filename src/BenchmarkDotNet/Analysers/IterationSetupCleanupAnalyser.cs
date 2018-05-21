using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public class IterationSetupCleanupAnalyser : AnalyserBase
    {
        public override string Id => "IterationSetupCleanup";
        public static readonly IAnalyser Default = new IterationSetupCleanupAnalyser();

        private IterationSetupCleanupAnalyser() { }

        public override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            var culprits = GetCulprits(report);
            if (culprits.Any())
            {
                string culpritsStr = string.Join(" and ", culprits);
                if (HasMemoryDiagnoser(summary))
                    yield return CreateWarning($"MemoryDiagnoser could provide inaccurate results because of the {culpritsStr}", report);
            }
        }

        private static List<string> GetCulprits(BenchmarkReport report)
        {
            var culprits = new List<string>(2);
            if (report.Benchmark.Target.IterationSetupMethod != null)
                culprits.Add("IterationSetup");
            if (report.Benchmark.Target.IterationCleanupMethod != null)
                culprits.Add("IterationCleanup");
            return culprits;
        }

        private static bool HasMemoryDiagnoser(Summary summary) => summary.Config.GetCompositeDiagnoser().Ids.Contains(MemoryDiagnoser.DiagnoserId);
    }
}