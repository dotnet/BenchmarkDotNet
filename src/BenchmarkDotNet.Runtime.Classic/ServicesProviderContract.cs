using System;
using BenchmarkDotNet.Full;
using BenchmarkDotNet.Mono;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet
{
    // namespace, class name and field name MUST NOT be changed (see ServicesProvider.Load)
    internal class ServicesProviderContract
    {
        internal static readonly Services Settings =
            Type.GetType("Mono.Runtime") != null
                ? new Services(
                    new MonoRuntimeInformation(),
                    new MonoDiagnosersLoader(),
                    new MonoResourcesService(),
                    _ => null,
                    (timeout, codegenMode, logOutput) => new InProcessExecutor(timeout, codegenMode, logOutput),
                    new DotNetStandardWorkarounds())
                : new Services(
                    new ClassicRuntimeInformation(),
                    new ClassicDiagnosersLoader(),
                    new ClassicResourcesService(),
                    DirtyAssemblyResolveHelper.Create,
                    (timeout, codegenMode, logOutput) => new InProcessExecutor(timeout, codegenMode, logOutput),
                    new DotNetStandardWorkarounds());
    }
}