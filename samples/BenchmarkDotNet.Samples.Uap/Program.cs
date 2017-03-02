using BenchmarkDotNet.Running;
using System.Reflection;

namespace BenchmarkDotNet.Samples.Uap
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}
