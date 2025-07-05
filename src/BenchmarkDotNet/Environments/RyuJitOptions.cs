namespace BenchmarkDotNet.Environments
{
    /// <summary>
    /// Configures the behavior of RyuJIT.
    /// </summary>
    public sealed class RyuJitOptions(
        bool minOpts = false,
        bool? tiered = null,
        bool? dynamicPGO = null,
        bool aggressiveTiering = true,
        int? tieredCallCountThreshold = 1)
    {
        public static readonly RyuJitOptions RuntimeDefault = new(aggressiveTiering: false, tieredCallCountThreshold: null);

        public static readonly RyuJitOptions AggressiveTiering = new();

        /// <summary>
        /// If <see langword="true"/>, the jit will compile methods with minimal optimizations (every method will be effectively tier0 jitted instead of tier1 jitted).
        /// </summary>
        public bool MinOpts { get; } = minOpts;

        /// <summary>
        /// Enable or disable tiered jit.
        /// </summary>
        /// <remarks>
        /// If <see cref="MinOpts"/> is true, this value has no effect and tiered jit is disabled.
        /// <para/>If unspecified, it is enabled by default in .Net Core 3.0+, disabled by default in .Net Core 2.X.
        /// </remarks>
        public bool? Tiered { get; } = tiered;

        /// <summary>
        /// Enable or disable dynamic profile-guided optimization.
        /// </summary>
        /// <remarks>
        /// If tiered jit is disabled, this value has no effect and dynamic PGO is disabled.
        /// <para/>If unspecified, it is enabled by default in .Net 8+, disabled by default in .Net 6 and 7.
        /// </remarks>
        public bool? DynamicPGO { get; } = dynamicPGO;

        /// <summary>
        /// If <see langword="true"/>, the jit will aggressively tier up methods.
        /// </summary>
        /// <remarks>
        /// If tiered jit is disabled, this value has no effect.
        /// </remarks>
        public bool Aggressive { get; } = aggressiveTiering;

        /// <summary>
        /// How many times must a method be invoked in order to be eligible for the next jit tier.
        /// </summary>
        /// <remarks>
        /// If tiered jit is disabled, this value has no effect.
        /// <para/>If <see cref="Aggressive"/> is <see langword="true"/>, this value will be ignored and <see langword="1"/> will be used instead.
        /// Otherwise, if this is unspecified, the default value is <see langword="30"/>.
        /// </remarks>
        public int? TieredCallCountThreshold { get; } = tieredCallCountThreshold;

        // We don't expose CallCountingDelayMs, because it's not useful for benchmarks to have any value larger than 0.
        // We also don't expose QuickJit or QuickJitForLoops, because it's rare for benchmarks to need it.
        // Users can still configure these via EnvironmentVariable if they really want to.
    }
}