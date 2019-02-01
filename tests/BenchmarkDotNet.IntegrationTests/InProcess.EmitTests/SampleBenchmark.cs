using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.IntegrationTests.InProcess.EmitTests
{
    public class SampleBenchmark
    {
        //[GlobalSetup]
        //public void GlobalSetup()
        //{ }

        [GlobalCleanup]
        public static void GlobalCleanup()
        {
        }

        //[IterationSetup]
        //public void IterationSetup()
        //{ }

        [IterationCleanup]
        public static void IterationCleanup()
        {
        }

        [Benchmark]
        public void VoidNoParamsCase()
        {
            Thread.Sleep(100);
        }

        [Benchmark, Arguments(1)]
        public string ReturnSingleArgCase(int i)
        {
            Thread.Sleep(100);
            return i.ToString();
        }

        [Benchmark, Arguments(123.0, 4, "5", null)]
        public CustomStructNonConsumable ReturnManyArgsCase(ref double i, int j, string k, object l)
        {
            Thread.Sleep(100);
            return default;
        }

        private int refValueHolder;

        [Benchmark, Arguments(123.0, 4, "5", null)]
        public ref int RefReturnManyArgsCase(ref double i, int j, string k, object l)
        {
            Thread.Sleep(100);
            return ref refValueHolder;
        }

        [Benchmark, Arguments(12)]
        public Task<int> TaskSample(long arg)
        {
            Thread.Sleep(100);
            return Task.FromResult(0);
        }
    }
}