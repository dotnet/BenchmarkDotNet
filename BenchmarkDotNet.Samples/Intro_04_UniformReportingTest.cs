using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{
    using System;
    using System.Threading;

    /// <summary>
    /// A special benchmark to see that operation times are reported in uniform time units
    /// and align nicely. These benchmarks produce widely different results but still we
    /// should be able to compare them side-by-side without doing any mental translations
    /// between nanoseconds to microseconds to milliseconds.
    /// 
    /// Expected output along these lines:
    /// 
    ///   Method |        AvrTime |      StdDev |       op/s |
    /// -------- |--------------- |------------ |----------- |
    ///  Slower1 |      1.8112 us |   0.2015 us | 552,120.14 |
    ///  Slower2 |      2.4149 us |   0.2425 us | 414,095.82 |
    ///  Slower3 |      9.0558 us |   0.3052 us | 110,426.47 |
    ///  Slower4 |     81.2006 us |   1.2585 us |  12,315.18 |
    ///  Slower5 |  4,148.1727 us | 740.3897 us |     241.07 |
    ///  Slower6 | 15,047.0119 us |  99.5290 us |      66.46 |
    /// 
    /// As can be seen, all time units are microseconds, even though the two slowest of them
    /// is in the milliseconds territory.
    /// </summary>
    [BenchmarkTask(mode: BenchmarkMode.SingleRun)]
    public class Intro_04_UniformReportingTest
    {
        [Benchmark]
        public void Slower1()
        {
            Iterate(5);
        }

        [Benchmark]
        public void Slower2()
        {
            Iterate(100);
        }

        [Benchmark]
        public void Slower3()
        {
            Iterate(1000);
        }

        [Benchmark]
        public void Slower4()
        {
            Iterate(10000);
        }

        [Benchmark]
        public void Slower5()
        {
            Iterate(500000);
        }

        [Benchmark]
        public void Slower6()
        {
            Iterate(500000);
            Thread.Sleep(10);
        }

        private static int[] Iterate(int arraySize)
        {
            var rand = new Random();
            var array = new int[arraySize];
            for (var i = 0; i < arraySize; i++)
            {
                array[i] = rand.Next();
            }

            return array;
        }
    }
}