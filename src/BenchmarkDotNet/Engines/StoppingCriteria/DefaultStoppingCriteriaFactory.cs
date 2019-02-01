using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Engines
{
    public class DefaultStoppingCriteriaFactory
    {
        public static readonly DefaultStoppingCriteriaFactory Instance = new DefaultStoppingCriteriaFactory();

        private const int MinOverheadIterationCount = 4;
        internal const int MaxOverheadIterationCount = 10;

        public virtual IStoppingCriteria CreateWarmupWorkload(Job job, IResolver resolver, RunStrategy runStrategy)
        {
            var count = job.ResolveValueAsNullable(RunMode.WarmupCountCharacteristic);
            if (count.HasValue && count.Value != EngineResolver.ForceAutoWarmup || runStrategy == RunStrategy.Monitoring)
                return new FixedStoppingCriteria(count ?? 0);

            int minIterationCount = job.ResolveValue(RunMode.MinWarmupIterationCountCharacteristic, resolver);
            int maxIterationCount = job.ResolveValue(RunMode.MaxWarmupIterationCountCharacteristic, resolver);
            return new AutoWarmupStoppingCriteria(minIterationCount, maxIterationCount);
        }

        public virtual IStoppingCriteria CreateWarmupOverhead()
        {
            return new AutoWarmupStoppingCriteria(MinOverheadIterationCount, MaxOverheadIterationCount);
        }

        public virtual IStoppingCriteria CreateWarmup(Job job, IResolver resolver, IterationMode mode, RunStrategy runStrategy)
        {
            switch (mode)
            {
                case IterationMode.Overhead:
                    return CreateWarmupOverhead();
                case IterationMode.Workload:
                    return CreateWarmupWorkload(job, resolver, runStrategy);
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
    }
}