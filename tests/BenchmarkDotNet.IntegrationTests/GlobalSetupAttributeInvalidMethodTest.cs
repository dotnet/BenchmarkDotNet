using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.IntegrationTests
{
    public class GlobalSetupAttributeInvalidMethodTest : BenchmarkTestExecutor
    {
        public GlobalSetupAttributeInvalidMethodTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GlobalSetupAttributeMethodsMustHaveNoParameters()
        {
            var summary = CanExecute<GlobalSetupAttributeInvalidMethod>(fullValidation: false);

            Assert.True(summary.HasCriticalValidationErrors);
            Assert.Contains(summary.ValidationErrors,
                error => error.Message == "GlobalSetup method GlobalSetup has incorrect signature.\nMethod shouldn't have any arguments.");
        }

        public class GlobalSetupAttributeInvalidMethod
        {
            [GlobalSetup]
            public void GlobalSetup(int someParameters) // [GlobalSetup] methods must have no parameters
            {
                Console.WriteLine("// ### GlobalSetup called ###");
            }

            [Benchmark]
            public void Benchmark()
            {
                Thread.Sleep(5);
            }
        }
    }
}
