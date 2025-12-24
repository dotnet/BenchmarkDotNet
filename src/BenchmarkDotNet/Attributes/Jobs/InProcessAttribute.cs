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
    internal static Job GetJob(InProcessToolchainType toolchainType, bool executeOnSeparateThread)
    {
        if (toolchainType == InProcessToolchainType.Auto)
        {
            toolchainType = RuntimeInformation.IsAot
                ? InProcessToolchainType.NoEmit
                : InProcessToolchainType.Emit;
        }
        return toolchainType == InProcessToolchainType.Emit
            ? Job.Default.WithToolchain(new InProcessEmitToolchain(new() { ExecuteOnSeparateThread = executeOnSeparateThread }))
            : Job.Default.WithToolchain(new InProcessNoEmitToolchain(new() { ExecuteOnSeparateThread = executeOnSeparateThread }));
    }
}