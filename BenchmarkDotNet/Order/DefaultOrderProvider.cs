using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Order
{
    public class DefaultOrderProvider : IOrderProvider
    {
        public static readonly IOrderProvider Instance = new DefaultOrderProvider();

        private readonly IComparer<ParameterInstances> paramsComparer = ParameterComparer.Instance;
        private readonly IComparer<IJob> jobComparer = JobComparer.Instance;
        private readonly IComparer<Target> targetComparer;

        private readonly SummaryOrderPolicy summaryOrderPolicy;

        public DefaultOrderProvider(
            SummaryOrderPolicy summaryOrderPolicy = SummaryOrderPolicy.Default, 
            MethodOrderPolicy methodOrderPolicy = MethodOrderPolicy.Declared)
        {
            this.summaryOrderPolicy = summaryOrderPolicy;
            targetComparer = new TargetComparer(methodOrderPolicy);
        }

        public virtual IEnumerable<Benchmark> GetExecutionOrder(Benchmark[] benchmarks)
        {
            var list = benchmarks.ToList();
            list.Sort(new BenchmarkComparer(paramsComparer, jobComparer, targetComparer));
            return list;
        }

        public virtual IEnumerable<Benchmark> GetSummaryOrder(Benchmark[] benchmarks, Summary summary)
        {
            switch (summaryOrderPolicy)
            {
                case SummaryOrderPolicy.FastestToSlowest:
                    return benchmarks.OrderBy(b => summary[b].ResultStatistics.Median);
                case SummaryOrderPolicy.SlowestToFastest:
                    return benchmarks.OrderByDescending(b => summary[b].ResultStatistics.Median);
                default:
                    return GetExecutionOrder(benchmarks);
            }
        }

        public string GetGroupKey(Benchmark benchmark, Summary summary) =>
            summaryOrderPolicy == SummaryOrderPolicy.Default
            ? benchmark.Parameters.FullInfo
            : null;

        private class BenchmarkComparer : IComparer<Benchmark>
        {
            private readonly IComparer<ParameterInstances> paramsComparer;
            private readonly IComparer<IJob> jobComparer;
            private readonly IComparer<Target> targetComparer;

            public BenchmarkComparer(IComparer<ParameterInstances> paramsComparer, IComparer<IJob> jobComparer, IComparer<Target> targetComparer)
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
                string.CompareOrdinal(x.FullInfo, y.FullInfo)
            }.FirstOrDefault(c => c != 0);
        }
    }
}