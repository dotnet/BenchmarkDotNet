using System.Collections.Generic;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    internal class EngineWarmupStage : EngineStage
    {
        private readonly IEngine engine;

        public EngineWarmupStage(IEngine engine) : base(engine) => this.engine = engine;

        public IReadOnlyList<Measurement> RunOverhead(long invokeCount, int unrollFactor)
            => Run(invokeCount, IterationMode.Overhead, unrollFactor, RunStrategy.Throughput);

        public IReadOnlyList<Measurement> RunWorkload(long invokeCount, int unrollFactor, RunStrategy runStrategy)
            => Run(invokeCount, IterationMode.Workload, unrollFactor, runStrategy);

        internal IReadOnlyList<Measurement> Run(long invokeCount, IterationMode iterationMode, int unrollFactor, RunStrategy runStrategy)
        {
            var criteria = DefaultStoppingCriteriaFactory.Instance.CreateWarmup(engine.TargetJob, engine.Resolver, iterationMode, runStrategy);
            return Run(criteria, invokeCount, iterationMode, IterationStage.Warmup, unrollFactor);
        }
    }
}