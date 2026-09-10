using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

/// <summary>
/// An <see cref="IToolchain"/> to run the benchmarks in-process by reflection.
/// </summary>
[PublicAPI]
public sealed class InProcessNoEmitToolchain : IToolchain
{
    public static readonly InProcessNoEmitToolchain Default = new(RuntimeInformation.GetCurrentRuntime(), InProcessNoEmitSettings.Default);

    private readonly InProcessNoEmitSettings settings;

    private InProcessNoEmitToolchain(Runtime runtime, InProcessNoEmitSettings settings)
    {
        this.settings = settings;
        Runtime = runtime;
        Generator = new InProcessNoEmitGenerator();
        Builder = new InProcessNoEmitBuilder();
        Executor = new InProcessNoEmitExecutor(settings.ExecuteOnSeparateThread, settings.BenchmarkActionFactory);
    }

    /// <summary>Returns an in-process toolchain for the given settings, associated with the current runtime.</summary>
    public static InProcessNoEmitToolchain From(InProcessNoEmitSettings settings)
        => From(RuntimeInformation.GetCurrentRuntime(), settings);

    /// <summary>Returns an in-process toolchain for the given runtime and settings.</summary>
    public static InProcessNoEmitToolchain From(Runtime runtime, InProcessNoEmitSettings settings)
        => runtime.Equals(Default.Runtime) && settings.Equals(InProcessNoEmitSettings.Default)
        ? Default
        : new(runtime, settings);

    public async IAsyncEnumerable<ValidationError> ValidateAsync(BenchmarkCase benchmarkCase, IResolver resolver)
    {
        await foreach (var error in InProcessValidator.ValidateAsync(benchmarkCase).ConfigureAwait(false))
        {
            yield return error;
        }

        if (benchmarkCase.Descriptor.WorkloadMethod.HasAttribute<AsyncCallerTypeAttribute>() == true)
        {
            yield return new ValidationError(false,
                $"{nameof(InProcessNoEmitToolchain)} does not support overriding the async caller type via [AsyncCallerType]. It will be ignored.",
                benchmarkCase);
        }
    }

    public Runtime Runtime { get; }

    public IGenerator Generator { get; }

    public IBuilder Builder { get; }

    public IExecutor Executor { get; }

    public bool IsInProcess => true;

    public override string ToString() => GetType().Name;

    public override bool Equals(object? obj)
        => obj is InProcessNoEmitToolchain other
        && Runtime.Equals(other.Runtime)
        && settings.Equals(other.settings);

    public override int GetHashCode() => HashCode.Combine(Runtime, settings);
}
