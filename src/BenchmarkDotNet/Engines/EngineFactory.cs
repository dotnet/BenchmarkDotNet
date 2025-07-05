namespace BenchmarkDotNet.Engines
{
    public class EngineFactory : IEngineFactory
    {
        public IEngine CreateReadyToRun(EngineParameters engineParameters)
        {
            // TODO: Move GlobalSetup/Cleanup to Engine.Run.
            engineParameters.GlobalSetupAction?.Invoke(); // whatever the settings are, we MUST call global setup here, the global cleanup is part of Engine's Dispose

            return new Engine(engineParameters);
        }
    }
}
