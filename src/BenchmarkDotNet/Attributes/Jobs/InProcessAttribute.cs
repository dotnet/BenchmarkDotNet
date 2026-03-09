using System;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

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

        return toolchainType == InProcessToolchainType.Emit
            ? baseJob.WithToolchain(new InProcessEmitToolchain(new() { ExecuteOnSeparateThread = executeOnSeparateThread }))
            : baseJob.WithToolchain(new InProcessNoEmitToolchain(new() { ExecuteOnSeparateThread = executeOnSeparateThread }));
    }
}