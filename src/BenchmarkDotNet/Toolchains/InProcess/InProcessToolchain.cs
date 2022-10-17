using System;
using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    /// <summary>
    ///     A toolchain to run the benchmarks in-process.
    /// </summary>
    /// <seealso cref="IToolchain" />
    [Obsolete("Please use BenchmarkDotNet.Toolchains.InProcess.NoEmit.* classes")]
    public sealed class InProcessToolchain : IToolchain
    {
        /// <summary>The default toolchain instance.</summary>
        public static readonly IToolchain Instance = new InProcessToolchain(true);

        /// <summary>The toolchain instance without output logging.</summary>
        public static readonly IToolchain DontLogOutput = new InProcessToolchain(false);

        /// <summary>Initializes a new instance of the <see cref="InProcessToolchain" /> class.</summary>
        /// <param name="logOutput"><c>true</c> if the output should be logged.</param>
        public InProcessToolchain(bool logOutput) : this(
            InProcessExecutor.DefaultTimeout,
            BenchmarkActionCodegen.ReflectionEmit,
            logOutput)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="InProcessToolchain" /> class.</summary>
        /// <param name="timeout">Timeout for the run.</param>
        /// <param name="codegenMode">Describes how benchmark action code is generated.</param>
        /// <param name="logOutput"><c>true</c> if the output should be logged.</param>
        public InProcessToolchain(TimeSpan timeout, BenchmarkActionCodegen codegenMode, bool logOutput)
        {
            Generator = new InProcessGenerator();
            Builder = new InProcessBuilder();
            Executor = new InProcessExecutor(timeout, codegenMode, logOutput);
        }

        /// <summary>Validates the specified benchmark.</summary>
        /// <param name="benchmarkCase">The benchmark.</param>
        /// <param name="resolver">The resolver.</param>
        /// <returns>Collection of validation errors, when not empty means that toolchain does not support provided benchmark.</returns>
        public IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver) =>
            InProcessValidator.Validate(benchmarkCase);

        /// <summary>Name of the toolchain.</summary>
        /// <value>The name of the toolchain.</value>
        public string Name => nameof(InProcessToolchain);

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