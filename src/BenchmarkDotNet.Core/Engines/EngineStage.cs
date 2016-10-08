using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    public class EngineStage
    {
        private readonly IEngine engine;

        protected EngineStage(IEngine engine)
        {
            this.engine = engine;
        }

        protected Job TargetJob => engine.TargetJob;
        protected bool IsDiagnoserAttached => engine.IsDiagnoserAttached;

        protected Measurement RunIteration(IterationMode mode, int index, long invokeCount, int unrollFactor)
        {
            if (invokeCount % unrollFactor != 0)
                throw new ArgumentOutOfRangeException($"InvokeCount({invokeCount}) should be a multiple of UnrollFactor({unrollFactor}).");
            return engine.RunIteration(new IterationData(mode, index, invokeCount, unrollFactor));
        }

        protected bool ShouldRunAuto(ICharacteristic<int> iterationCount) => iterationCount.IsDefault;

        protected void WriteLine() => engine.WriteLine();
        protected void WriteLine(string line) => engine.WriteLine(line);
    }
}