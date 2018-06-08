using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tests.XUnit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ExtraAttributesForEntryMethodTests : BenchmarkTestExecutor
    {
        public ExtraAttributesForEntryMethodTests(ITestOutputHelper output) : base(output)
        {
        }

        [FactClassicDotNetOnly("STAThread attribute is not respected in netcoreapp https://github.com/dotnet/coreclr/issues/13688")]
        public void UserCanMarkBenchmarkAsRequiringSTA() => CanExecute<RequiresSTA>();

        public class RequiresSTA
        {
            [Benchmark, System.STAThread]
            public void CheckForSTA()
            {
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    throw new ThreadStateException("The current threads apartment state is not STA");
                }
            }
        }
    }
}
