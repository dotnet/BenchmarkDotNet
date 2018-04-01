
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Achievements
{
    public class PerfImprovementAchievement : IAchievementJudge
    {
        public IEnumerable<Achievement> GetAchievements(BenchmarkReport benchmarkResults, BenchmarkState lastState)
        {
            if (lastState == null || benchmarkResults.ResultStatistics == null) yield break;
            
            // time
            if (benchmarkResults.ResultStatistics.Median * 1.2 < lastState.Runtime.Min)
                yield return new Achievement("Improvement by 20%", "Your runtime is reduced by 20% percent (compared to previous minimum). Good start, I'm sure you can do better ;)", 1);
            if (benchmarkResults.ResultStatistics.Median * 1.7 < lastState.Runtime.Min)
                yield return new Achievement("Improvement by 70%", "Your runtime is reduced by 70% percent (compared to previous minimum). Nice.", 2);
            if (benchmarkResults.ResultStatistics.Median * 2 < lastState.Runtime.Min)
                yield return new Achievement("Improvement 2-times", "Your benchmark runs two-times faster (compared to previous minimum). Great, isn't it?", 3);
            if (benchmarkResults.ResultStatistics.Median * 5 < lastState.Runtime.Min)
                yield return new Achievement("Improvement 5-times", "Your benchmark runs five-times faster (compared to previous minimum). Cool, cheapo computers are going to love your software :)", 4);
            if (benchmarkResults.ResultStatistics.Median * 10 < lastState.Runtime.Min)
                yield return new Achievement("Improvement 10-times", "Your benchmark runs ten-times faster (compared to previous minimum). Epic, feels fresh now, doesn't it?", 5);
            if (benchmarkResults.ResultStatistics.Median * 30 < lastState.Runtime.Min)
                yield return new Achievement("Improvement 30-times", "Your benchmark runs 30-times faster (compared to previous minimum). Congrats, this looks like a wizard's job.", 6);
            if (benchmarkResults.ResultStatistics.Median * 100 < lastState.Runtime.Min)
                yield return new Achievement("Improvement 100-times", "Your benchmark runs 100-times faster (compared to previous minimum). Men, well done, but the previous version must have been soooo terribly slow.", 7);
            
            if (benchmarkResults.ResultStatistics.Median * 1000 < lastState.Runtime.Min)
                yield return new Achievement("Improvement 1000-times", "Your benchmark runs 1000-times faster (compared to previous minimum). Have you removed Thread.Sleep(1000) from an empty method?", 8);
            
            if (benchmarkResults.ResultStatistics.Median * 10000 < lastState.Runtime.Min)
                yield return new Achievement("Improvement 10000-times", "Your benchmark runs 10000-times faster (compared to previous minimum). I don't trust that, you are cheating.", 9);

            if (benchmarkResults.GcStats.BytesAllocatedPerOperation < lastState.Allocated.Last * 0.95 &&
                benchmarkResults.ResultStatistics.Median * 0.85 > lastState.Runtime.Last)
            {
                yield return new Achievement("GC brainwash", "You are reducing allocations just before cool kids on twitter do it. Performance is what really matters, reducing allocations is one of the ways, have a look at the numbers", -1, allowRepeated: true);
                yield break; // don't give any gc-related achievements
            }
            
            if (benchmarkResults.GcStats.BytesAllocatedPerOperation < lastState.Allocated.Min)
                yield return new Achievement("Less allocations", $"You have reduced allocations of your benchmark. \"Thanks for help\" - Your GC", 1);
            if (benchmarkResults.GcStats.Gen2Collections == 0 && benchmarkResults.ResultStatistics?.Median > 10)
                yield return new Achievement("No GC2", "No GC2 was triggered by your benchmark. \"🏖 🍹️\" - Your GC");
            if (lastState.Allocated.Min > 0 && benchmarkResults.GcStats.BytesAllocatedPerOperation == 0)
                yield return new Achievement("Zero allocations", $"You benchmark does not allocate any single byte from GC heap. \"Oh no, what am I going to do?\" - Your GC", 10);
            
            if (benchmarkResults.GcStats.BytesAllocatedPerOperation < lastState.Allocated.Min * 0.8)
                yield return new Achievement("-20% allocations", "Your benchmark allocates 30% less, great job", 1);
            if (benchmarkResults.GcStats.BytesAllocatedPerOperation < lastState.Allocated.Min * 0.6)
                yield return new Achievement("-40% allocations", "Your benchmark allocates 40% less, struct em all", 2);
            if (benchmarkResults.GcStats.BytesAllocatedPerOperation * 3 > lastState.Allocated.Min)
                yield return new Achievement("1/3 allocations", "Your benchmark allocates 3-times less. Where are you hiding these objects?", 3);
            if (benchmarkResults.GcStats.BytesAllocatedPerOperation * 10 > lastState.Allocated.Min)
                yield return new Achievement("1/10 allocations", "Your benchmark allocates 10-times less. .NET is going to give the GC holiday.", 4);
            if (benchmarkResults.GcStats.BytesAllocatedPerOperation * 50 > lastState.Allocated.Min)
                yield return new Achievement("1/50 allocations", "Your benchmark allocates 50-times less. The GC will become too lazy and fatty soon...", 5);
        }
    }

    public class StrangeStuffAchievements : IAchievementJudge
    {
        public IEnumerable<Achievement> GetAchievements(BenchmarkReport benchmarkResults, BenchmarkState lastState)
        {
            if (benchmarkResults.Benchmark.Parameters.Count > 0)
                yield return new Achievement("Parameters are cool", "Hmm, parameter are cool, don't they? ;)", 1);
            if (benchmarkResults.Benchmark.FolderInfo.Length > 250)
                yield return new Achievement("Kill FS", $"Let's kill the filesystem with {Encoding.UTF8.GetBytes(benchmarkResults.Benchmark.FolderInfo).Length} bytes long name :D - {benchmarkResults.Benchmark.FolderInfo}");
            if (benchmarkResults.GcStats.BytesAllocatedPerOperation > 100 * 1000 * 1000)
                yield return new Achievement("100M allocations", $"100MB allocated during benchmark? Are you benchmarking Visual Studio or what?", 7);
            if (benchmarkResults.GcStats.BytesAllocatedPerOperation > 100 * 1000 * 1000)
                yield return new Achievement("1M allocations", $"1MB allocated during your microbenchmark. I mean 'macro-benchmark'");
            if (benchmarkResults.ResultStatistics?.ConfidenceInterval.Lower * 3 < benchmarkResults.ResultStatistics?.ConfidenceInterval.Upper)
                yield return new Achievement("Fuzzy test", "So huge gap between min and max? You sure you did the benchmark right?", 5);
        }
    }
}