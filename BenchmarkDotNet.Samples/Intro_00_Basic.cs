using System.Threading;

namespace BenchmarkDotNet.Samples
{
    // It is very easy to use BenchmarkDotNet. You should just create a class
    public class Intro_00_Basic
    {
        // And define a method with the Benchmark attribute
        [Benchmark]
        public void Sleep()
        {
            Thread.Sleep(100);
        }


        // You can write description for a method.
        [Benchmark("Thread.Sleep(100)")]
        public void SleepWithDescription()
        {
            Thread.Sleep(100);
        }

        // Now you can run this benchmark competition with help of BenchmarkRunner:
        // new BenchmarkRunner().RunCompetition(new Intro_00_Basic());
    }
}