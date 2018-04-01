using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Achievements
{
    public class AchievementAnalyser : IAnalyser
    {
        public static readonly AchievementAnalyser Default = new AchievementAnalyser();
        
        IEnumerable<(BenchmarkState newState, BenchmarkState lastState)> UpdateBenchmarkStates(IEnumerable<BenchmarkReport> reports)
        {
            var result = new List<(BenchmarkState newState, BenchmarkState lastState)>();
            var lastStates = LocalStateStorage.ReadSomething<IDictionary<string, BenchmarkState>>("benchmark-stats") ?? new Dictionary<string, BenchmarkState>();
            foreach (var r in reports)
            {
                lastStates.TryGetValue(r.Benchmark.DisplayInfo, out var lastState);
                var newstate = BenchmarkState.NewBlock(lastState, r.ResultStatistics?.Median ?? 0, r.GcStats.BytesAllocatedPerOperation);
                lastStates[r.Benchmark.DisplayInfo] = newstate;
                result.Add((newstate, lastState));
            }
            LocalStateStorage.SaveSomething("benchmark-stats", lastStates.ToDictionary(k => k.Key, k => (object)k.Value));
            return result;
        }
        
        ConcurrentStack<IAchievementJudge> judges = new ConcurrentStack<IAchievementJudge>(new IAchievementJudge[] { new PerfImprovementAchievement(), new StrangeStuffAchievements() });
        public void AddJudge(IAchievementJudge judge) => judges.Push(judge);
        
        public List<(Achievement achievement, BenchmarkReport benchmark)> GetResults(BenchmarkReport[] benchmarkReports)
        {
            var benchmarkStates = benchmarkReports.Zip(UpdateBenchmarkStates(benchmarkReports),
                (a, b) => (a, b)).ToDictionary(k => k.a, k => k.b.lastState);
            
            var result = new List<(Achievement achievement, BenchmarkReport benchmark)>();
            var gainedAchievements = new Lazy<IDictionary<string, AchievementGainedRecord>>(() =>
                LocalStateStorage.ReadSomething<IDictionary<string, AchievementGainedRecord>>("gained-achievements") ?? new Dictionary<string, AchievementGainedRecord>());
            foreach (var benchmarkReport in benchmarkReports)
            {
                foreach (var j in judges)
                {
                    benchmarkStates.TryGetValue(benchmarkReport, out var lastState);
                    var a = j.GetAchievements(benchmarkReport, lastState).ToArray();
                    foreach (var achievement in a.Where(aa => aa.AllowRepeated))
                        result.Add((achievement, benchmarkReport));
                    var maxEpic = a.OrderByDescending(aa => aa.Epicness).FirstOrDefault();
                    if (maxEpic != null && !gainedAchievements.Value.ContainsKey(maxEpic.Name))
                    {
                        result.Add((maxEpic, benchmarkReport));
                        gainedAchievements.Value.Add(maxEpic.Name, new AchievementGainedRecord());
                    }
                }
            }
            if (gainedAchievements.IsValueCreated)
                LocalStateStorage.SaveSomething("gained-achievements", gainedAchievements.Value.ToDictionary(a => a.Key, a => (object)a.Value));

            if (result.Count > 0)
                LocalStateStorage.ModifySomething("achievement-count", (int i) => i + result.Count(r => r.achievement.Epicness >= 0));

            return result;
        }

        public string Id => @"

                     v v v v v v v v v v v          🎈
                >>>> 🎉 New Achievements 🎉 <<<<   🎈    😊
                     ^ ^ ^ ^ ^ ^ ^ ^ ^ ^ ^         🎈";
        public IEnumerable<Conclusion> Analyse(Summary summary)
        {
            return this.GetResults(summary.Reports)
                   .Select(a => Conclusion.CreateHint(Id, $"{(a.achievement.Epicness > 5 ? "😎" : "")} {a.achievement.Name}: {a.achievement.Description}", a.benchmark));
        }
    }

    public class MinMaxLast<TValue>
        where TValue: IComparable<TValue>
    {
        public TValue Min { get; set; }
        public TValue Max { get; set; }
        public TValue Last { get; set; }

        public MinMaxLast() { }
        public MinMaxLast(TValue oneValue) : this(oneValue, oneValue, oneValue) { }

        public MinMaxLast(TValue min, TValue max, TValue last)
        {
            Min = min;
            Max = max;
            Last = last;
        }

        public MinMaxLast<TValue> WithNewValue(TValue value) =>
            new MinMaxLast<TValue>(
                this.Min.CompareTo(value) < 0 ? value : this.Min,
                this.Max.CompareTo(value) > 0 ? value : this.Max,
                value
            );
    }

    public class BenchmarkState
    {
        public int Counter { get; set; }
        public MinMaxLast<double> Runtime { get; set; }
        public MinMaxLast<long> Allocated { get; set; }

        public BenchmarkState() { }

        public BenchmarkState(int counter, MinMaxLast<double> runtime, MinMaxLast<long> allocated)
        {
            Counter = counter;
            Runtime = runtime;
            Allocated = allocated;
        }

        public static BenchmarkState NewBlock(BenchmarkState lastState, double runtime, long allocated)
        {
            return new BenchmarkState(
                (lastState?.Counter ?? 0) + 1,
                lastState?.Runtime?.WithNewValue(runtime) ?? new MinMaxLast<double>(runtime),
                lastState?.Allocated?.WithNewValue(allocated) ?? new MinMaxLast<long>(allocated)
                );
        }
    }

    public class Achievement
    {
        public string Name { get; }
        public string Description { get; }
        public double Epicness { get; }
        public bool AllowRepeated { get; }

        public Achievement(string name, string description, double epicness = 0.0, bool allowRepeated = false)
        {
            Name = name;
            Description = description;
            Epicness = epicness;
            AllowRepeated = allowRepeated;
        }
    }

    public class AchievementGainedRecord
    {
    }
    
    public interface IAchievementJudge
    {
        IEnumerable<Achievement> GetAchievements(BenchmarkReport benchmarkResults, BenchmarkState lastState);
    }
}