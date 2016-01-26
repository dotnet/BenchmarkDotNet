using BenchmarkDotNet.Loggers;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class OutputLogger : AccumulationLogger
    {
        private readonly ITestOutputHelper testOutputHelper;

        public OutputLogger(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        public override void Write(LogKind logKind, string format, params object[] args)
        {
            testOutputHelper.WriteLine(format, args);
            base.Write(logKind, format, args);
        }
    }
}