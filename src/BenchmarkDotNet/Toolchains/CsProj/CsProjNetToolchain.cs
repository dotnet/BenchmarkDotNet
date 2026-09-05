using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Toolchains.CsProj;

public abstract class CsProjNetToolchain(string name, Runtime runtime, DotNetCliSettings settings, IGenerator generator, IBuilder builder, IExecutor executor)
    : Toolchain(name, runtime, generator, builder, executor), IHasSettings
{
    internal DotNetCliSettings Settings => settings;
    ISettings IHasSettings.Settings => Settings;

    public override async IAsyncEnumerable<ValidationError> ValidateAsync(BenchmarkCase benchmarkCase, IResolver resolver)
    {
        await foreach (var validationError in base.ValidateAsync(benchmarkCase, resolver).ConfigureAwait(false))
        {
            yield return validationError;
        }

        if (benchmarkCase.Job.HasValue(EnvironmentMode.JitCharacteristic) && benchmarkCase.Job.ResolveValue(EnvironmentMode.JitCharacteristic, resolver) == Jit.LegacyJit)
        {
            yield return new ValidationError(true,
                $"{GetType().Name} supports only RyuJit, benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                benchmarkCase);
        }
        if (benchmarkCase.Job.ResolveValue(GcMode.CpuGroupsCharacteristic, resolver))
        {
            yield return new ValidationError(true,
                $"Currently {GetType().Name} does not support CpuGroups (app.config does), benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                benchmarkCase);
        }
        if (benchmarkCase.Job.ResolveValue(GcMode.AllowVeryLargeObjectsCharacteristic, resolver))
        {
            yield return new ValidationError(true,
                $"Currently {GetType().Name} does not support gcAllowVeryLargeObjects (app.config does), benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                benchmarkCase);
        }

        var benchmarkAssembly = benchmarkCase.Descriptor.Type.Assembly;
        if (benchmarkAssembly.IsLinqPad())
        {
            yield return new ValidationError(true,
                $"Currently {GetType().Name} does not support LINQPad 6+. Please use {nameof(InProcessEmitToolchain)} instead.",
                benchmarkCase);
        }

        foreach (var validationError in DotNetSdkValidator.ValidateCoreSdks(settings.CliPath, benchmarkCase))
        {
            yield return validationError;
        }
    }

    public override bool Equals(object? obj)
        => obj is CsProjNetToolchain other
        && other.GetType() == GetType()
        && Runtime.Equals(other.Runtime)
        && Settings.Equals(other.Settings);

    public override int GetHashCode() => HashCode.Combine(GetType(), Runtime, Settings);
}
