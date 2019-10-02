using System;
using System.Collections.Generic;
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

        internal List<Measurement> Run(IStoppingCriteria criteria, long invokeCount, IterationMode mode, IterationStage stage, int unrollFactor)
        {
            var measurements = new List<Measurement>(criteria.MaxIterationCount);
            int iterationCounter = 0;
            while (true)
            {
                iterationCounter++;
                measurements.Add(RunIteration(mode, stage, iterationCounter, invokeCount, unrollFactor));
                if (criteria.Evaluate(measurements).IsFinished)
                    break;
            }

            WriteLine();

            return measurements;
        }

        protected void WriteLine() => engine.WriteLine();
    }
}