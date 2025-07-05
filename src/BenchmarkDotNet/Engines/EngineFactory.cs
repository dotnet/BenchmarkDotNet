namespace BenchmarkDotNet.Engines
{
    public class EngineFactory : IEngineFactory
    {
        public IEngine CreateReadyToRun(EngineParameters engineParameters)
        {
            var engine = new Engine(engineParameters);

            // TODO: Move GlobalSetup/Cleanup to Engine.Run.
            engine.Parameters.GlobalSetupAction.Invoke(); // whatever the settings are, we MUST call global setup here, the global cleanup is part of Engine's Dispose

            return engine;
        }
    }
}
