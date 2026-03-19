using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using System;

namespace BenchmarkDotNet.Attributes;

public enum InProcessToolchainType
{
    Auto,
    Emit,
    NoEmit,
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
public class InProcessAttribute(
    InProcessToolchainType toolchainType = InProcessToolchainType.Auto,
    bool executeOnSeparateThread = true)
    : JobConfigBaseAttribute(GetJob(toolchainType, executeOnSeparateThread))
{
    private static Job GetJob(InProcessToolchainType toolchainType, bool executeOnSeparateThread)
        => GetJob(Job.Default, toolchainType, executeOnSeparateThread);

    internal static Job GetJob(Job baseJob, InProcessToolchainType toolchainType, bool executeOnSeparateThread)
    {
        if (toolchainType == InProcessToolchainType.Auto)
        {
            toolchainType = RuntimeInformation.IsAot
                ? InProcessToolchainType.NoEmit
                : InProcessToolchainType.Emit;
        }

        IToolchain toolchain = toolchainType switch
        {
            InProcessToolchainType.Emit => new InProcessEmitToolchain(new() { ExecuteOnSeparateThread = executeOnSeparateThread }),
            InProcessToolchainType.NoEmit => new InProcessNoEmitToolchain(new() { ExecuteOnSeparateThread = executeOnSeparateThread }),
            _ => throw new ArgumentOutOfRangeException(nameof(toolchainType))
        };

        var job = baseJob.WithToolchain(toolchain);
        if (!job.HasValue(CharacteristicObject.IdCharacteristic))
            job = job.WithId("InProcess");

        return job.Freeze();
    }
}