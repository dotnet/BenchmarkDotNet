namespace BenchmarkDotNet.Engines
{
    public static class IterationModeExtensions
    {
        public static bool IsIdle(this IterationMode mode) 
            => mode == IterationMode.IdleWarmup || mode == IterationMode.IdleTarget || mode == IterationMode.IdleJitting;
    }
}