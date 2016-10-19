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
            if (engineParameters.IdleAction == null)
                throw new ArgumentNullException(nameof(engineParameters.IdleAction));
            if(engineParameters.TargetJob == null)
                throw new ArgumentNullException(nameof(engineParameters.TargetJob));

            return new Engine(
                engineParameters.IdleAction,
                engineParameters.MainAction,
                engineParameters.TargetJob,
                engineParameters.SetupAction,
                engineParameters.CleanupAction,
                engineParameters.OperationsPerInvoke,
                engineParameters.IsDiagnoserAttached);
        }
    }
}