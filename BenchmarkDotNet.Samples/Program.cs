using Benchmarks;

namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main()
        {
            BenchmarkSettings.Instance.DefaultWarmUpIterationCount = 3;
            BenchmarkSettings.Instance.DefaultResultIterationCount = 5;
            BenchmarkSettings.Instance.DetailedMode = true;
            new AttributesSampleCompetition().Run();
            ConsoleHelper.WriteLineDefault("----------------------------------------");
            new CustomSampleProgram().Run();
        }
    }
}
