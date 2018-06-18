using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Order
{
    public class DefaultOrderer : IOrderer
    {
        public static readonly IOrderer Instance = new DefaultOrderer();

        private readonly IComparer<ParameterInstances> paramsComparer = ParameterComparer.Instance;
        private readonly IComparer<Job> jobComparer = JobComparer.Instance;
        private readonly IComparer<Target> targetComparer;
        private readonly IComparer<Benchmark> benchmarkComparer;
        private readonly IComparer<IGrouping<string, Benchmark>> logicalGroupComparer;

        private readonly SummaryOrderPolicy summaryOrderPolicy;

        public DefaultOrderer(
            SummaryOrderPolicy summaryOrderPolicy = SummaryOrderPolicy.Default, 
            MethodOrderPolicy methodOrderPolicy = MethodOrderPolicy.Declared)
        {
            this.summaryOrderPolicy = summaryOrderPolicy;
            targetComparer = new TargetComparer(methodOrderPolicy);
            benchmarkComparer = new BenchmarkComparer(paramsComparer, jobComparer, targetComparer);
            logicalGroupComparer = new LogicalGroupComparer(benchmarkComparer);
        }

        public virtual IEnumerable<Benchmark> GetExecutionOrder(Benchmark[] benchmarks)
        {
            var list = benchmarks.ToList();
            list.Sort(benchmarkComparer);
            return list;
        }

        public virtual IEnumerable<Benchmark> GetSummaryOrder(Benchmark[] benchmarks, Summary summary)
        {
            var benchmarkLogicalGroups = benchmarks.GroupBy(b => GetLogicalGroupKey(summary.Config, benchmarks, b));
            foreach (var logicalGroup in GetLogicalGroupOrder(benchmarkLogicalGroups))
            foreach (var benchmark in GetSummaryOrderForGroup(logicalGroup.ToArray(), summary))
                yield return benchmark;
        }
        
        protected virtual IEnumerable<Benchmark> GetSummaryOrderForGroup(Benchmark[] benchmarks, Summary summary)
        {            
            switch (summaryOrderPolicy)
            {
                case SummaryOrderPolicy.FastestToSlowest:
                    return benchmarks.OrderBy(b => summary[b].ResultStatistics.Mean);
                case SummaryOrderPolicy.SlowestToFastest:
                    return benchmarks.OrderByDescending(b => summary[b].ResultStatistics.Mean);
                case SummaryOrderPolicy.Method:
                    return benchmarks.OrderBy(b => b.Target.MethodDisplayInfo);
                case SummaryOrderPolicy.Declared:
                    return benchmarks;
                default:
                    return GetExecutionOrder(benchmarks);
            }
        }

        public string GetHighlightGroupKey(Benchmark benchmark)
        {
            switch (summaryOrderPolicy)
            {
                case SummaryOrderPolicy.Default:
                    return benchmark.Parameters.DisplayInfo;
                case SummaryOrderPolicy.Method:
                    return benchmark.Target.MethodDisplayInfo;
                default:
                    return null;
            }
        }

        public string GetLogicalGroupKey(IConfig config, Benchmark[] allBenchmarks, Benchmark benchmark)
        {
            var rules = new HashSet<BenchmarkLogicalGroupRule>(config.GetLogicalGroupRules());
            if (allBenchmarks.Any(b => b.Job.Meta.IsBaseline))
            {
                rules.Add(BenchmarkLogicalGroupRule.ByMethod);
                rules.Add(BenchmarkLogicalGroupRule.ByParams);
            }
            if (allBenchmarks.Any(b => b.Target.Baseline))
            {
                rules.Add(BenchmarkLogicalGroupRule.ByJob);
                rules.Add(BenchmarkLogicalGroupRule.ByParams);
            }

            var keys = new List<string>();            
            if (rules.Contains(BenchmarkLogicalGroupRule.ByMethod))
                keys.Add(benchmark.Target.DisplayInfo);
            if (rules.Contains(BenchmarkLogicalGroupRule.ByJob))
                keys.Add(benchmark.Job.DisplayInfo);
            if (rules.Contains(BenchmarkLogicalGroupRule.ByParams))
                keys.Add(benchmark.Parameters.DisplayInfo);
            if (rules.Contains(BenchmarkLogicalGroupRule.ByCategory))
                keys.Add(string.Join(",", benchmark.Target.Categories));

            string logicalGroupKey = string.Join("-", keys.Where(key => key != string.Empty));
            return logicalGroupKey == string.Empty ? "*" : logicalGroupKey;
        }

        public virtual IEnumerable<IGrouping<string, Benchmark>> GetLogicalGroupOrder(IEnumerable<IGrouping<string, Benchmark>> logicalGroups)
        {
            var list = logicalGroups.ToList();
            list.Sort(logicalGroupComparer);
            return list;
        }

        public bool SeparateLogicalGroups => true;

        private class BenchmarkComparer : IComparer<Benchmark>
        {
            private readonly IComparer<ParameterInstances> paramsComparer;
            private readonly IComparer<Job> jobComparer;
            private readonly IComparer<Target> targetComparer;

            public BenchmarkComparer(IComparer<ParameterInstances> paramsComparer, IComparer<Job> jobComparer, IComparer<Target> targetComparer)
            {
                this.targetComparer = targetComparer;
                this.jobComparer = jobComparer;
                this.paramsComparer = paramsComparer;
            }

            public int Compare(Benchmark x, Benchmark y) => new[]
            {
                paramsComparer?.Compare(x.Parameters, y.Parameters) ?? 0,
                jobComparer?.Compare(x.Job, y.Job) ?? 0,
                targetComparer?.Compare(x.Target, y.Target) ?? 0,
                string.CompareOrdinal(x.DisplayInfo, y.DisplayInfo)
            }.FirstOrDefault(c => c != 0);
        }

        private class LogicalGroupComparer : IComparer<IGrouping<string, Benchmark>>
        {
            private IComparer<Benchmark> benchmarkComparer;

            public LogicalGroupComparer(IComparer<Benchmark> benchmarkComparer) => this.benchmarkComparer = benchmarkComparer;

            public int Compare(IGrouping<string, Benchmark> x, IGrouping<string, Benchmark> y) => benchmarkComparer.Compare(x.First(), y.First());
        }
    }
}
