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
        protected AccuracyMode TargetAccuracy => TargetJob.Accuracy;
        protected IClock TargetClock => engine.Resolver.Resolve(TargetJob.Infrastructure.Clock);
        protected IResolver Resolver => engine.Resolver;

        protected Measurement RunIteration(IterationMode mode, int index, long invokeCount)
        {
            return engine.RunIteration(new IterationData(mode, index, invokeCount));
        }

        protected void WriteLine() => engine.WriteLine();

        protected void WriteLine(string line) => engine.WriteLine(line);
    }
}