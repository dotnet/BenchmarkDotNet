using System;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    public class EngineStage
    {
        private readonly IEngine engine;

        protected EngineStage(IEngine engine) => this.engine = engine;

        protected Job TargetJob => engine.TargetJob;

        protected Measurement RunIteration(IterationMode mode, IterationStage stage, int index, long invokeCount, int unrollFactor)
        {
            if (invokeCount % unrollFactor != 0)
                throw new ArgumentOutOfRangeException($"InvokeCount({invokeCount}) should be a multiple of UnrollFactor({unrollFactor}).");
            return engine.RunIteration(new IterationData(mode, stage, index, invokeCount, unrollFactor));
        }

        protected void WriteLine() => engine.WriteLine();
    }
}