using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit
{
    /// <summary>
    /// An <see cref="IToolchain"/> to run the benchmarks in-process by reflection.
    /// </summary>
    [PublicAPI]
    public sealed class InProcessNoEmitToolchain : IToolchain
    {
        /// <summary>A toolchain instance with default settings.</summary>
        public static readonly IToolchain Default = new InProcessNoEmitToolchain(new() { ExecuteOnSeparateThread = true });

        /// <summary>Initializes a new instance of the <see cref="InProcessNoEmitToolchain" /> class.</summary>
        /// <param name="settings">The settings to use for the toolchain.</param>
        public InProcessNoEmitToolchain(InProcessNoEmitSettings settings)
        {
            Generator = new InProcessNoEmitGenerator();
            Builder = new InProcessNoEmitBuilder();
            Executor = new InProcessNoEmitExecutor(settings.ExecuteOnSeparateThread);
        }

        public IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            foreach (var error in InProcessValidator.Validate(benchmarkCase))
            {
                yield return error;
            }

            if (benchmarkCase.Descriptor.WorkloadMethod.ReturnType.HasAttribute<AsyncCallerTypeAttribute>() == true)
            {
                yield return new ValidationError(false,
                    $"{nameof(InProcessNoEmitToolchain)} does not support overriding the async caller type via [AsyncCallerType]. It will be ignored.",
                    benchmarkCase);
            }

        }

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