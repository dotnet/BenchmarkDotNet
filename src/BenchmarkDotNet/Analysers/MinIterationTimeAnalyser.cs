using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public class MinIterationTimeAnalyser : AnalyserBase
    {
        private static readonly TimeInterval MinSufficientIterationTime = 100 * TimeInterval.Millisecond;

        public override string Id => "MinIterationTime";
        public static readonly IAnalyser Default = new MinIterationTimeAnalyser();

        private MinIterationTimeAnalyser()
        {
        }

        public override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            var target = report.AllMeasurements.Where(m => m.IterationMode == IterationMode.MainTarget).ToArray();
            if (target.IsEmpty())
                yield break;
            var minActualIterationTime = TimeInterval.FromNanoseconds(target.Min(m => m.Nanoseconds));
            if (minActualIterationTime < MinSufficientIterationTime)
                yield return CreateWarning($"MinIterationTime = {minActualIterationTime} which is very small. It's recommended to increase it.", report);
        }
    }
}