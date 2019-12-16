using BenchmarkDotNet.Running;

namespace _BenchmarkProjectName_
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<$(BenchmarkName)>();
        }
    }
}