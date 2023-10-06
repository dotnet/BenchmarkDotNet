using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples
{
    public class Program
    {
        public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}