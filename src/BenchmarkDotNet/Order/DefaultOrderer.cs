using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Order
{
    [SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
    public class DefaultOrderer : IOrderer
    {
        public static readonly IOrderer Instance = new DefaultOrderer();

        private readonly IComparer<ParameterInstances> paramsComparer = ParameterComparer.Instance;
        private readonly IComparer<Job> jobComparer = JobComparer.Instance;
        private readonly IComparer<BenchmarkCase> benchmarkComparer;
        private readonly IComparer<IGrouping<string, BenchmarkCase>> logicalGroupComparer;

        public SummaryOrderPolicy SummaryOrderPolicy { get; }
        public MethodOrderPolicy MethodOrderPolicy { get; }

        public DefaultOrderer(
            SummaryOrderPolicy summaryOrderPolicy = SummaryOrderPolicy.Default, 
            MethodOrderPolicy methodOrderPolicy = MethodOrderPolicy.Declared)
        {
            SummaryOrderPolicy = summaryOrderPolicy;
            MethodOrderPolicy = methodOrderPolicy;
            IComparer<Descriptor> targetComparer = new DescriptorComparer(methodOrderPolicy);
            benchmarkComparer = new BenchmarkComparer(paramsComparer, jobComparer, targetComparer);
            logicalGroupComparer = new LogicalGroupComparer(benchmarkComparer);
        }

        [PublicAPI]
        public virtual IEnumerable<BenchmarkCase> GetExecutionOrder(ImmutableArray<BenchmarkCase> benchmarkCases)
        {
            var list = benchmarkCases.ToList();
            list.Sort(benchmarkComparer);
            return list;
        }

        public virtual IEnumerable<BenchmarkCase> GetSummaryOrder(ImmutableArray<BenchmarkCase> benchmarksCases, Summary summary)
        {
            var benchmarkLogicalGroups = benchmarksCases.GroupBy(b => GetLogicalGroupKey(benchmarksCases, b));
            foreach (var logicalGroup in GetLogicalGroupOrder(benchmarkLogicalGroups))
            foreach (var benchmark in GetSummaryOrderForGroup(logicalGroup.ToImmutableArray(), summary))
                yield return benchmark;
        }
        
        protected virtual IEnumerable<BenchmarkCase> GetSummaryOrderForGroup(ImmutableArray<BenchmarkCase> benchmarksCase, Summary summary)
        {            
            switch (SummaryOrderPolicy)
            {
                case SummaryOrderPolicy.FastestToSlowest:
                    return benchmarksCase.OrderBy(b => summary[b]?.ResultStatistics?.Mean ?? 0d);
                case SummaryOrderPolicy.SlowestToFastest:
                    return benchmarksCase.OrderByDescending(b => summary[b]?.ResultStatistics?.Mean ?? 0d);
                case SummaryOrderPolicy.Method:
                    return benchmarksCase.OrderBy(b => b.Descriptor.WorkloadMethodDisplayInfo);
                case SummaryOrderPolicy.Declared:
                    return benchmarksCase;
                default:
                    return GetExecutionOrder(benchmarksCase);
            }
        }

        public string GetHighlightGroupKey(BenchmarkCase benchmarkCase)
        {
            switch (SummaryOrderPolicy)
            {
                case SummaryOrderPolicy.Default:
                    return benchmarkCase.Parameters.DisplayInfo;
                case SummaryOrderPolicy.Method:
                    return benchmarkCase.Descriptor.WorkloadMethodDisplayInfo;
                default:
                    return null;
            }
        }

        public string GetLogicalGroupKey(ImmutableArray<BenchmarkCase> allBenchmarksCases, BenchmarkCase benchmarkCase)
        {
            var rules = new HashSet<BenchmarkLogicalGroupRule>(benchmarkCase.Config.GetLogicalGroupRules());
            bool hasJobBaselines = allBenchmarksCases.Any(b => b.Job.Meta.Baseline);
            bool hasDescriptorBaselines = allBenchmarksCases.Any(b => b.Descriptor.Baseline);
            if (hasJobBaselines)
            {
                rules.Add(BenchmarkLogicalGroupRule.ByMethod);
                rules.Add(BenchmarkLogicalGroupRule.ByParams);
            }            
            if (hasDescriptorBaselines)
            {
                rules.Add(BenchmarkLogicalGroupRule.ByJob);
                rules.Add(BenchmarkLogicalGroupRule.ByParams);
            }
            if (hasJobBaselines && hasDescriptorBaselines)
            {
                rules.Remove(BenchmarkLogicalGroupRule.ByMethod);
                rules.Remove(BenchmarkLogicalGroupRule.ByJob);
            }

            var keys = new List<string>();            
            if (rules.Contains(BenchmarkLogicalGroupRule.ByMethod))
                keys.Add(benchmarkCase.Descriptor.DisplayInfo);
            if (rules.Contains(BenchmarkLogicalGroupRule.ByJob))
                keys.Add(benchmarkCase.Job.DisplayInfo);
            if (rules.Contains(BenchmarkLogicalGroupRule.ByParams))
                keys.Add(benchmarkCase.Parameters.ValueInfo);
            if (rules.Contains(BenchmarkLogicalGroupRule.ByCategory))
                keys.Add(string.Join(",", benchmarkCase.Descriptor.Categories));

            string logicalGroupKey = string.Join("-", keys.Where(key => key != string.Empty));
            return logicalGroupKey == string.Empty ? "*" : logicalGroupKey;
        }

        public virtual IEnumerable<IGrouping<string, BenchmarkCase>> GetLogicalGroupOrder(IEnumerable<IGrouping<string, BenchmarkCase>> logicalGroups)
        {
            var list = logicalGroups.ToList();
            list.Sort(logicalGroupComparer);
            return list;
        }

        public bool SeparateLogicalGroups => true;

        private class BenchmarkComparer : IComparer<BenchmarkCase>
        {
            private readonly IComparer<ParameterInstances> paramsComparer;
            private readonly IComparer<Job> jobComparer;
            private readonly IComparer<Descriptor> targetComparer;

            public BenchmarkComparer(IComparer<ParameterInstances> paramsComparer, IComparer<Job> jobComparer, IComparer<Descriptor> targetComparer)
            {
                this.targetComparer = targetComparer;
                this.jobComparer = jobComparer;
                this.paramsComparer = paramsComparer;
            }

            public int Compare(BenchmarkCase x, BenchmarkCase y)
            {
                if (x == null && y == null) return 0;
                if (x != null && y == null) return 1;
                if (x == null) return -1;
                return new[]
                {
                    paramsComparer?.Compare(x.Parameters, y.Parameters) ?? 0,
                    jobComparer?.Compare(x.Job, y.Job) ?? 0,
                    targetComparer?.Compare(x.Descriptor, y.Descriptor) ?? 0,
                    string.CompareOrdinal(x.DisplayInfo, y.DisplayInfo)
                }.FirstOrDefault(c => c != 0);
            }
        }

        private class LogicalGroupComparer : IComparer<IGrouping<string, BenchmarkCase>>
        {
            private readonly IComparer<BenchmarkCase> benchmarkComparer;

            public LogicalGroupComparer(IComparer<BenchmarkCase> benchmarkComparer) => this.benchmarkComparer = benchmarkComparer;

            public int Compare(IGrouping<string, BenchmarkCase> x, IGrouping<string, BenchmarkCase> y)
            {
                if (x == null && y == null) return 0;
                if (x != null && y == null) return 1;
                if (x == null) return -1;
                return benchmarkComparer.Compare(x.First(), y.First());
            }
        }
    }
}
