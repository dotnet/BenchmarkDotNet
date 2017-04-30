using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet
{
    // namespace, class name and field name MUST NOT be changed (see ServicesProvider.Load)
    internal class ServicesProviderContract
    {
        internal static readonly Services Settings =
            new Services(
                new RuntimeInformation(), 
                new DiagnosersLoader(), 
                new ResourcesService(), 
                _ => null,
                (timeout, codegenMode, logOutput) => new InProcessExecutor(timeout, codegenMode, logOutput),
                new DoNetStandardWorkarounds());
    }
}