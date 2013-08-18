using System;

namespace BenchmarkDotNet
{
    public class BenchmarkInfo
    {
        public BenchmarkRunList WarmUp { get; set; }
        public BenchmarkRunList Result { get; set; }

        public void Print()
        {
            Console.WriteLine("WarmUp:");
            WarmUp.Print();
            Console.WriteLine();
            Console.WriteLine("Result:");
            Result.Print();            
        }
    }
}