using BenchmarkDotNet.Attributes;
using System.Threading;

namespace BenchmarkDotNet.Samples
{
    [DryJob]
    public class IntroParamsAllValues
    {
        public enum CustomEnum
        {
            One = 1,
            Two,
            Three
        }

        [ParamsAllValues]
        public CustomEnum E { get; set; }

        [ParamsAllValues]
        public bool? B { get; set; }

        [Benchmark]
        public void Benchmark()
        {
            Thread.Sleep(
                (int)E * 100 +
                (B == true ? 20 : B == false ? 10 : 0));
        }
    }
}