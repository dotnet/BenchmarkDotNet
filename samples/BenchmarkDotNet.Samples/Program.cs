using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace BenchmarkDotNet.Samples
{
    class Program
    {
        //static void Main()
        //{
        //    new System.Threading.Thread(() =>
        //    {
        //        var src = new int[1_000];
        //        for (; ; ) Array.Sort(src);
        //    }).Start();
        //    for (; ; ) { Console.WriteLine("GC start"); GC.Collect(); Console.WriteLine("GC end"); Thread.Sleep(1); }
        //}
        static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }

    public class SimpleTest
    {
        private int[] array = Enumerable.Range(0, 1000).ToArray();

        [Benchmark]
        public void Sort() => Array.Sort(array);

        [Benchmark]
        public void Reverse() => Array.Reverse(array);

        [Benchmark]
        public void BubbleSort()
        {
            var arr = array;
            int temp;
            for (int write = 0; write < arr.Length; write++)
            {
                for (int sort = 0; sort < arr.Length - 1; sort++)
                {
                    if (arr[sort] > arr[sort + 1])
                    {
                        temp = arr[sort + 1];
                        arr[sort + 1] = arr[sort];
                        arr[sort] = temp;
                    }
                }
            }
        }
    }
}

namespace Hacky
{
    public class Hack
    {
        [Benchmark]
        public void InvokedInstead() { GC.Collect(); Thread.Sleep(1); }
    }
}