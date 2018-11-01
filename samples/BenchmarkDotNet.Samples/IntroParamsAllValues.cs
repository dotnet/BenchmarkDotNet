using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [DryJob]
    public class IntroParamsAllValues
    {
        public enum CustomEnum
        {
            A,
            BB,
            CCC
        }

        [ParamsAllValues]
        public CustomEnum E { get; set; }

        [ParamsAllValues]
        public bool? B { get; set; }

        [Benchmark]
        public void Benchmark()
        {
            Thread.Sleep(
                E.ToString().Length * 100 +
                (B == true ? 20 : B == false ? 10 : 0));
        }
    }
}