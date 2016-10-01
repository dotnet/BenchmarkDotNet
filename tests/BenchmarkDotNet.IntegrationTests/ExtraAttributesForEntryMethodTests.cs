using System.Threading;
using BenchmarkDotNet.Attributes;
#if !CORE
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ExtraAttributesForEntryMethodTests : BenchmarkTestExecutor
    {
        public ExtraAttributesForEntryMethodTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void UserCanMarkBenchmarkAsRequiringSTA()
        {
            CanExecute<RequiresSTA>();
        }

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
#endif