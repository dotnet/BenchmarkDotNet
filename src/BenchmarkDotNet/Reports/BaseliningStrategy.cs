using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Reports
{
    internal class BaseliningStrategy
    {
        private static readonly BaseliningStrategy Disabled = new BaseliningStrategy(false, false);
        private static readonly BaseliningStrategy DescriptorOnly = new BaseliningStrategy(true, false);
        private static readonly BaseliningStrategy JobOnly = new BaseliningStrategy(false, true);
        private static readonly BaseliningStrategy DescriptorAndJob = new BaseliningStrategy(true, true);

        private readonly bool useDescriptors, useJobs;

        public BaseliningStrategy(bool useDescriptors, bool useJobs)
        {
            this.useDescriptors = useDescriptors;
            this.useJobs = useJobs;
        }

        public static BaseliningStrategy Create(ImmutableArray<BenchmarkCase> benchmarkCases)
        {
            bool hasDescriptorBaselines = benchmarkCases.Any(b => b.Descriptor.Baseline);
            bool hasJobBaselines = benchmarkCases.Any(b => b.Job.Meta.Baseline);
            if (hasDescriptorBaselines && hasJobBaselines)
                return DescriptorAndJob;
            if (hasDescriptorBaselines)
                return DescriptorOnly;
            if (hasJobBaselines)
                return JobOnly;
            return Disabled;
        }

        public bool IsBaseline(BenchmarkCase benchmark)
        {
            if (!useDescriptors && !useJobs)
                return false;
            bool result = true;
            if (useDescriptors)
                result &= benchmark.Descriptor.Baseline;
            if (useJobs)
                result &= benchmark.Job.Meta.Baseline;
            return result;
        }
    }
}