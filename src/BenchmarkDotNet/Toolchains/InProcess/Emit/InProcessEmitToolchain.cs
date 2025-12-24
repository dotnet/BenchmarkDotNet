using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit
{
    /// <summary>
    /// An <see cref="IToolchain"/> to run the benchmarks in-process by emitting IL.
    /// </summary>
    [PublicAPI]
    public class InProcessEmitToolchain : Toolchain
    {
        /// <summary>A toolchain instance with default settings.</summary>
        public static readonly IToolchain Default = new InProcessEmitToolchain(new() { ExecuteOnSeparateThread = true });

        /// <summary>Initializes a new instance of the <see cref="InProcessEmitToolchain" /> class.</summary>
        /// <param name="settings">The settings to use for the toolchain.</param>
        public InProcessEmitToolchain(InProcessEmitSettings settings) : base(
            nameof(InProcessEmitToolchain),
            new InProcessEmitGenerator(),
            new InProcessEmitBuilder(),
            new InProcessEmitExecutor(settings.ExecuteOnSeparateThread))
        {
        }

        public override bool IsInProcess => true;
    }
}