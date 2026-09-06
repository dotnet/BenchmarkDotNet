using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System.Diagnostics;
using System.Text;

namespace BenchmarkDotNet.Toolchains.Mono;

internal sealed class LegacyMonoExecutor(LegacyMonoSettings settings) : Executor
{
    internal LegacyMonoSettings Settings => settings;

    protected override ProcessStartInfo CreateStartInfo(BenchmarkCase benchmarkCase, ArtifactsPaths artifactsPaths, string args, IResolver resolver)
    {
        var start = base.CreateStartInfo(benchmarkCase, artifactsPaths, args, resolver);
        start.FileName = settings.MonoPath?.FullName ?? "mono";
        start.Arguments = GetMonoArguments(benchmarkCase.Job, artifactsPaths.ExecutablePath, args, resolver);
        if (settings.MonoBclPath is not null)
        {
            start.EnvironmentVariables["MONO_PATH"] = settings.MonoBclPath.FullName;
        }
        return start;
    }

    private static string GetMonoArguments(Job job, string exePath, string args, IResolver resolver)
    {
        var arguments = job.HasValue(InfrastructureMode.ArgumentsCharacteristic)
            ? job.ResolveValue(InfrastructureMode.ArgumentsCharacteristic, resolver)!.OfType<MonoArgument>().ToArray()
            : [];

        // from mono --help: "Usage is: mono [options] program [program-options]"
        var builder = new StringBuilder(30);

        builder.Append(job.ResolveValue(EnvironmentMode.JitCharacteristic, resolver) == Jit.Llvm ? "--llvm" : "--nollvm");

        foreach (var argument in arguments)
        {
            builder.Append($" {argument.TextRepresentation}");
        }

        builder.Append($" \"{exePath}\" ");
        builder.Append(args);

        return builder.ToString();
    }
}
