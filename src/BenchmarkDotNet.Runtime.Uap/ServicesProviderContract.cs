using System;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess;

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
                (Func<TimeSpan, BenchmarkActionCodegen, bool, IExecutor>)((timeout, codegenMode, logOutput) => new InProcessExecutor(timeout, codegenMode, logOutput)),
                new DotNetStandardWorkarounds(), 
                new BenchmarkConverter());
    }
}