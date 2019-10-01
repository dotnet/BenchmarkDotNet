namespace BenchmarkDotNet.Environments
{
    public enum Jit
    {
        /// <summary>
        /// Default
        /// <remarks>By default</remarks>
        /// </summary>
        Default,

        /// <summary>
        /// LegacyJIT
        /// <remarks>Supported only for Full Framework</remarks>
        /// </summary>
        LegacyJit,

        /// <summary>
        /// RyuJIT
        /// <remarks>Full Framework or CoreCLR</remarks>
        /// </summary>
        RyuJit,

        /// <summary>
        /// LLVM
        /// <remarks>Supported only for Mono</remarks>
        /// </summary>
        Llvm
    }
}