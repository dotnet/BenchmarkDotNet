using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit
{
    /// <summary>
    /// An <see cref="IToolchain"/> to run the benchmarks in-process by emitting IL.
    /// </summary>
    public sealed class InProcessEmitToolchain : Toolchain
    {
        public static readonly InProcessEmitToolchain Default = new(RuntimeInformation.GetCurrentRuntime(), InProcessEmitSettings.Default);

        private readonly InProcessEmitSettings settings;

        public override bool IsInProcess => true;

        private InProcessEmitToolchain(Runtime runtime, InProcessEmitSettings settings) : base(
            nameof(InProcessEmitToolchain),
            runtime,
            new InProcessEmitGenerator(),
            new InProcessEmitBuilder(),
            new InProcessEmitExecutor(settings.ExecuteOnSeparateThread))
        {
            this.settings = settings;
        }

        /// <summary>Returns an in-process toolchain for the given settings, associated with the current runtime.</summary>
        public static InProcessEmitToolchain From(InProcessEmitSettings settings)
            => From(RuntimeInformation.GetCurrentRuntime(), settings);

        /// <summary>Returns an in-process toolchain for the given runtime and settings.</summary>
        public static InProcessEmitToolchain From(Runtime runtime, InProcessEmitSettings settings)
            => runtime.Equals(Default.Runtime) && settings.Equals(InProcessEmitSettings.Default)
            ? Default
            : new(runtime, settings);

        public override bool Equals(object? obj)
            => obj is InProcessEmitToolchain other
            && Runtime.Equals(other.Runtime)
            && settings.Equals(other.settings);

        public override int GetHashCode() => HashCode.Combine(Runtime, settings);
    }
}
