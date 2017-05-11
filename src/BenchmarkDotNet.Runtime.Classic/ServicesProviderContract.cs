using System;
using BenchmarkDotNet.Full;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Mono;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet
{
    // namespace, class name and field name MUST NOT be changed (see ServicesProvider.Load)
    public class ServicesProviderContract
    {
        public static void Initialize() => ServicesProvider.Configure(Settings);

        internal static readonly ServicesContainer Settings =
            Type.GetType("Mono.Runtime") != null
                ? new ServicesContainer(
                    new MonoRuntimeInformation(),
                    new MonoDiagnosersLoader(),
                    new MonoResourcesService(),
                    (Func<ILogger, IDisposable>)(_ => null),
                    new DotNetStandardWorkarounds(),
                    new BenchmarkConverter())
                : new ServicesContainer(
                    new ClassicRuntimeInformation(),
                    new ClassicDiagnosersLoader(),
                    new ClassicResourcesService(),
                    (Func<ILogger, IDisposable>)DirtyAssemblyResolveHelper.Create,
                    new DotNetStandardWorkarounds(),
                    new BenchmarkConverter());
    }
}