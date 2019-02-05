using System;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.Parameters;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains
{
    [PublicAPI("Used by some of our Superusers that implement their own Toolchains (e.g. Kestrel team)")]
    public sealed class Executor : OutOfProcessExecutor
    {
        protected override (string fileName, string arguments) GetProcessStartArguments(ExecuteParameters parameters)
        {
            var runtime = parameters.BenchmarkCase.Job.ResolveValue(EnvironmentMode.RuntimeCharacteristic, parameters.Resolver);

            switch (runtime)
            {
                case ClrRuntime _:
                case CoreRuntime _:
                case CoreRtRuntime _:
                    return (parameters.BuildResult.ArtifactsPaths.ExecutablePath, parameters.BenchmarkId.ToArguments());
                case MonoRuntime mono:
                    return (mono.CustomPath ?? "mono", GetMonoArguments(parameters));
                default:
                    throw new NotSupportedException("Runtime = " + runtime);
            }
        }

        private static string GetMonoArguments(ExecuteParameters parameters)
        {
            var job = parameters.BenchmarkCase.Job;

            var arguments = job.HasValue(InfrastructureMode.ArgumentsCharacteristic)
                ? job.ResolveValue(InfrastructureMode.ArgumentsCharacteristic, parameters.Resolver).OfType<MonoArgument>().ToArray()
                : Array.Empty<MonoArgument>();

            // from mono --help: "Usage is: mono [options] program [program-options]"
            var builder = new StringBuilder(30);

            builder.Append(job.ResolveValue(EnvironmentMode.JitCharacteristic, parameters.Resolver) == Jit.Llvm ? "--llvm" : "--nollvm");

            foreach (var argument in arguments)
                builder.Append($" {argument.TextRepresentation}");

            builder.Append($" \"{parameters.BuildResult.ArtifactsPaths.ExecutablePath}\" ");
            builder.Append(parameters.BenchmarkId.ToArguments()); // benchmarkID must always be the last argument

            return builder.ToString();
        }
    }
}