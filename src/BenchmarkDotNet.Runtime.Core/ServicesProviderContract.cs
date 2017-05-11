using System;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet
{
    // namespace, class name and field name MUST NOT be changed (see ServicesProvider.Load)
    public class ServicesProviderContract
    {
        public static void Initialize() => ServicesProvider.Configure(Settings);

        internal static readonly ServicesContainer Settings =
            new ServicesContainer(
                new RuntimeInformation(),
                new DiagnosersLoader(),
                new ResourcesService(),
                (Func<ILogger, IDisposable>)(_ => null),
                new DotNetStandardWorkarounds(),
                new BenchmarkConverter());
    }
}