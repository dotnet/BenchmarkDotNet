using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public class ConstantFoldingAnalyzer: AnalyserBase
    {
        public override string Id => "ConstantFolding";
        
        public static readonly IAnalyser Default = new ConstantFoldingAnalyzer();

        public override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            if (report.AllMeasurements.Any(x => x.IterationMode == IterationMode.Result && x.Nanoseconds == 0))
                yield return CreateWarning($"It seems that in {report.Benchmark.Target.DisplayInfo} takes place constant folding, values were zeroed");
        }
    }
}