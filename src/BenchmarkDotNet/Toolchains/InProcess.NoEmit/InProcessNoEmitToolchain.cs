using System;

using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit
{
    /// <summary>
    ///     A toolchain to run the benchmarks in-process (no emit).
    /// </summary>
    /// <seealso cref="IToolchain" />
    [PublicAPI]
    public sealed class InProcessNoEmitToolchain : IToolchain
    {
        /// <summary>The default toolchain instance.</summary>
        public static readonly IToolchain Instance = new InProcessNoEmitToolchain(true);

        /// <summary>The toolchain instance without output logging.</summary>
        public static readonly IToolchain DontLogOutput = new InProcessNoEmitToolchain(false);

        /// <summary>Initializes a new instance of the <see cref="InProcessNoEmitToolchain" /> class.</summary>
        /// <param name="logOutput"><c>true</c> if the output should be logged.</param>
        public InProcessNoEmitToolchain(bool logOutput) : this(
            InProcessNoEmitExecutor.DefaultTimeout,
            logOutput)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="InProcessNoEmitToolchain" /> class.</summary>
        /// <param name="timeout">Timeout for the run.</param>
        /// <param name="logOutput"><c>true</c> if the output should be logged.</param>
        public InProcessNoEmitToolchain(TimeSpan timeout, bool logOutput)
        {
            Generator = new InProcessNoEmitGenerator();
            Builder = new InProcessNoEmitBuilder();
            Executor = new InProcessNoEmitExecutor(timeout, logOutput);
        }

        /// <summary>Determines whether the specified benchmark is supported.</summary>
        /// <param name="benchmarkCase">The benchmark.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="resolver">The resolver.</param>
        /// <returns><c>true</c> if the benchmark can be run with the toolchain.</returns>
        public bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver) =>
            InProcessValidator.IsSupported(benchmarkCase, logger);

        /// <summary>Name of the toolchain.</summary>
        /// <value>The name of the toolchain.</value>
        public string Name => nameof(InProcessNoEmitToolchain);

        /// <summary>The generator.</summary>
        /// <value>The generator.</value>
        public IGenerator Generator { get; }

        /// <summary>The builder.</summary>
        /// <value>The builder.</value>
        public IBuilder Builder { get; }

        /// <summary>The executor.</summary>
        /// <value>The executor.</value>
        public IExecutor Executor { get; }

        public bool IsInProcess => true;

        /// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString() => GetType().Name;
    }
}