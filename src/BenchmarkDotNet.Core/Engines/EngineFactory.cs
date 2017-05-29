using System;

namespace BenchmarkDotNet.Engines
{
    // TODO: Default instance?
    public class EngineFactory : IEngineFactory
    {
        public IEngine Create(EngineParameters engineParameters)
        {
            if (engineParameters.MainAction == null)
                throw new ArgumentNullException(nameof(engineParameters.MainAction));
            if (engineParameters.Dummy1Action == null)
                throw new ArgumentNullException(nameof(engineParameters.Dummy1Action));
            if (engineParameters.Dummy2Action == null)
                throw new ArgumentNullException(nameof(engineParameters.Dummy2Action));
            if (engineParameters.Dummy3Action == null)
                throw new ArgumentNullException(nameof(engineParameters.Dummy3Action));
            if (engineParameters.IdleAction == null)
                throw new ArgumentNullException(nameof(engineParameters.IdleAction));
            if(engineParameters.TargetJob == null)
                throw new ArgumentNullException(nameof(engineParameters.TargetJob));

            return new Engine(
                engineParameters.Host,
                engineParameters.Dummy1Action,
                engineParameters.Dummy2Action,
                engineParameters.Dummy3Action,
                engineParameters.IdleAction,
                engineParameters.MainAction,
                engineParameters.TargetJob,
                engineParameters.GlobalSetupAction,
                engineParameters.GlobalCleanupAction,
                engineParameters.IterationSetupAction,
                engineParameters.IterationCleanupAction,
                engineParameters.OperationsPerInvoke);
        }
    }
}