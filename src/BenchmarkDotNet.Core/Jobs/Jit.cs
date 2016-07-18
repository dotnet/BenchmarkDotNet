namespace BenchmarkDotNet.Jobs
{
    public enum Jit
    {
        Host,
        LegacyJit,
        RyuJit,
        /// <summary>
        /// supported only for Mono
        /// </summary>
        Llvm
    }
}