using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

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

        protected override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            var target = report.AllMeasurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).ToArray();
            if (target.IsEmpty())
                yield break;
            var minActualIterationTime = TimeInterval.FromNanoseconds(target.Min(m => m.Nanoseconds));
            if (minActualIterationTime < MinSufficientIterationTime)
                yield return CreateWarning($"The minimum observed iteration time is {minActualIterationTime} which is very small. It's recommended to increase it to at least {MinSufficientIterationTime} using more operations.", report);
        }
    }
}