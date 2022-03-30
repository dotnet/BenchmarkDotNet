namespace BenchmarkDotNet.Environments
{
    public enum Platform
    {
        /// <summary>
        /// AnyCPU
        /// </summary>
        AnyCpu,

        /// <summary>
        /// x86
        /// </summary>
        X86,

        /// <summary>
        /// x64
        /// </summary>
        X64,

        /// <summary>
        /// ARM
        /// </summary>
        Arm,

        /// <summary>
        /// ARM64
        /// </summary>
        Arm64,

        /// <summary>
        /// Wasm
        /// </summary>
        Wasm,

        /// <summary>
        /// S390x
        /// </summary>
        S390x,

        /// <summary>
        /// LOONGARCH64
        /// </summary>
        LoongArch64
    }
}
