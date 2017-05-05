using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet
{
    // namespace, class name and field name MUST NOT be changed (see ServicesProvider.Load)
    public class ServicesProviderContract
    {
        public static void Initialize() => ServicesProvider.Configure(Settings);

        internal static readonly Services Settings =
            new Services(
                new RuntimeInformation(), 
                new DiagnosersLoader(), 
                new ResourcesService(), 
                _ => null,
                (timeout, codegenMode, logOutput) => new InProcessExecutor(timeout, codegenMode, logOutput),
                new DotNetStandardWorkarounds(),
                new BenchmarkConverter());
    }
}