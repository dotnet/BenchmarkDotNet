using System;
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

        private readonly IComparer<string[]> categoryComparer = CategoryComparer.Instance;
        private readonly IComparer<ParameterInstances> paramsComparer = ParameterComparer.Instance;
        private readonly IComparer<Job> jobComparer = JobComparer.Instance;
        private readonly IComparer<Descriptor> targetComparer;

        public SummaryOrderPolicy SummaryOrderPolicy { get; }
        public MethodOrderPolicy MethodOrderPolicy { get; }

        public DefaultOrderer(
            SummaryOrderPolicy summaryOrderPolicy = SummaryOrderPolicy.Default,
            MethodOrderPolicy methodOrderPolicy = MethodOrderPolicy.Declared)
        {
            SummaryOrderPolicy = summaryOrderPolicy;
            MethodOrderPolicy = methodOrderPolicy;
            targetComparer = new DescriptorComparer(methodOrderPolicy);
        }

        [PublicAPI]
        public virtual IEnumerable<BenchmarkCase> GetExecutionOrder(
            ImmutableArray<BenchmarkCase> benchmarkCases,
            IEnumerable<BenchmarkLogicalGroupRule>? order = null)
        {
            var benchmarkComparer = new BenchmarkComparer(categoryComparer, paramsComparer, jobComparer, targetComparer, order);
            var list = benchmarkCases.ToList();
            list.Sort(benchmarkComparer);
            return list;
        }

        public virtual IEnumerable<BenchmarkCase> GetSummaryOrder(ImmutableArray<BenchmarkCase> benchmarksCases, Summary summary)
        {
            var benchmarkLogicalGroups = benchmarksCases.GroupBy(b => GetLogicalGroupKey(benchmarksCases, b));
            foreach (var logicalGroup in GetLogicalGroupOrder(benchmarkLogicalGroups, benchmarksCases.FirstOrDefault()?.Config.GetLogicalGroupRules()))
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
                    return GetExecutionOrder(benchmarksCase, benchmarksCase.FirstOrDefault()?.Config.GetLogicalGroupRules());
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
            var explicitRules = benchmarkCase.Config.GetLogicalGroupRules().ToList();
            var implicitRules = new List<BenchmarkLogicalGroupRule>();
            bool hasJobBaselines = allBenchmarksCases.Any(b => b.Job.Meta.Baseline);
            bool hasDescriptorBaselines = allBenchmarksCases.Any(b => b.Descriptor.Baseline);
            if (hasJobBaselines)
            {
                implicitRules.Add(BenchmarkLogicalGroupRule.ByParams);
                implicitRules.Add(BenchmarkLogicalGroupRule.ByMethod);
            }
            if (hasDescriptorBaselines)
            {
                implicitRules.Add(BenchmarkLogicalGroupRule.ByParams);
                implicitRules.Add(BenchmarkLogicalGroupRule.ByJob);
            }
            if (hasJobBaselines && hasDescriptorBaselines)
            {
                implicitRules.Remove(BenchmarkLogicalGroupRule.ByMethod);
                implicitRules.Remove(BenchmarkLogicalGroupRule.ByJob);
            }

            var rules = new List<BenchmarkLogicalGroupRule>(explicitRules);
            foreach (var rule in implicitRules.Where(rule => !rules.Contains(rule)))
                rules.Add(rule);

            var keys = new List<string>();
            foreach (var rule in rules)
            {
                switch (rule)
                {
                    case BenchmarkLogicalGroupRule.ByMethod:
                        keys.Add(benchmarkCase.Descriptor.DisplayInfo);
                        break;
                    case BenchmarkLogicalGroupRule.ByJob:
                        keys.Add(benchmarkCase.Job.DisplayInfo);
                        break;
                    case BenchmarkLogicalGroupRule.ByParams:
                        keys.Add(benchmarkCase.Parameters.ValueInfo);
                        break;
                    case BenchmarkLogicalGroupRule.ByCategory:
                        keys.Add(string.Join(",", benchmarkCase.Descriptor.Categories));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(rule), rule, $"Not supported {nameof(BenchmarkLogicalGroupRule)}");
                }
            }

            string logicalGroupKey = string.Join("-", keys.Where(key => key != string.Empty));
            return logicalGroupKey == string.Empty ? "*" : logicalGroupKey;
        }

        public virtual IEnumerable<IGrouping<string, BenchmarkCase>> GetLogicalGroupOrder(
            IEnumerable<IGrouping<string, BenchmarkCase>> logicalGroups,
            IEnumerable<BenchmarkLogicalGroupRule>? order = null)
        {
            var benchmarkComparer = new BenchmarkComparer(categoryComparer, paramsComparer, jobComparer, targetComparer, order);
            var logicalGroupComparer = new LogicalGroupComparer(benchmarkComparer);
            var list = logicalGroups.ToList();
            list.Sort(logicalGroupComparer);
            return list;
        }

        public bool SeparateLogicalGroups => true;

        private class BenchmarkComparer : IComparer<BenchmarkCase>
        {
            private static readonly BenchmarkLogicalGroupRule[] DefaultOrder =
            {
                BenchmarkLogicalGroupRule.ByCategory,
                BenchmarkLogicalGroupRule.ByParams,
                BenchmarkLogicalGroupRule.ByJob,
                BenchmarkLogicalGroupRule.ByMethod
            };

            private readonly IComparer<string[]> categoryComparer;
            private readonly IComparer<ParameterInstances> paramsComparer;
            private readonly IComparer<Job> jobComparer;
            private readonly IComparer<Descriptor> targetComparer;
            private readonly List<BenchmarkLogicalGroupRule> order;

            public BenchmarkComparer(
                IComparer<string[]> categoryComparer,
                IComparer<ParameterInstances> paramsComparer,
                IComparer<Job> jobComparer,
                IComparer<Descriptor> targetComparer,
                IEnumerable<BenchmarkLogicalGroupRule> order)
            {
                this.categoryComparer = categoryComparer;
                this.targetComparer = targetComparer;
                this.jobComparer = jobComparer;
                this.paramsComparer = paramsComparer;

                this.order = new List<BenchmarkLogicalGroupRule>();
                foreach (var rule in (order ?? ImmutableArray<BenchmarkLogicalGroupRule>.Empty).Concat(DefaultOrder))
                    if (!this.order.Contains(rule))
                        this.order.Add(rule);
            }

            public int Compare(BenchmarkCase x, BenchmarkCase y)
            {
                if (x == null && y == null) return 0;
                if (x != null && y == null) return 1;
                if (x == null) return -1;

                foreach (var rule in order)
                {
                    int compare = rule switch
                    {
                        BenchmarkLogicalGroupRule.ByMethod => targetComparer?.Compare(x.Descriptor, y.Descriptor) ?? 0,
                        BenchmarkLogicalGroupRule.ByJob => jobComparer?.Compare(x.Job, y.Job) ?? 0,
                        BenchmarkLogicalGroupRule.ByParams => paramsComparer?.Compare(x.Parameters, y.Parameters) ?? 0,
                        BenchmarkLogicalGroupRule.ByCategory => categoryComparer?.Compare(x.Descriptor.Categories, y.Descriptor.Categories) ?? 0,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    if (compare != 0)
                        return compare;
                }
                return string.CompareOrdinal(x.DisplayInfo, y.DisplayInfo);
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