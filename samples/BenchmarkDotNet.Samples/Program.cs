using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main(string[] args) //=> BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        {
            BenchmarkRunner.Run<MathBenchmarks>();
        }
    }

    [Config(typeof(JitsConfig))]
    [DisassemblyDiagnoser]
    public class MathBenchmarks
    {
        private class JitsConfig : ManualConfig
        {
            public JitsConfig()
            {
                Add(Job.Default.With(Jit.RyuJit).With(Platform.X64).WithId("Ryu x64"));
            }
        }
        
        //[Benchmark(Baseline = true)]
        public double CreateUnitVectorReturnX()
        {
            return new UnitVector(1, 2).X;
        }

        //[Benchmark]
        public double Return1()
        {
            return 1;
        }

        public struct UnitVector
        {
            public readonly double X;
            public readonly double Y;

            public UnitVector(double x, double y)
            {
                X = x;
                Y = y;
            }
        }
        
        [Params(2)]
        public int A { get; set; }

        public int B;

        [Benchmark]
        public double Abs() => Math.Abs(A);
        
        //[Benchmark]
        public double Sqrt14() 
            => Math.Sqrt(1) + Math.Sqrt(2) + Math.Sqrt(3) + Math.Sqrt(4) +
               Math.Sqrt(5) + Math.Sqrt(6) + Math.Sqrt(7) + Math.Sqrt(8) +
               Math.Sqrt(9) + Math.Sqrt(10) + Math.Sqrt(11) + Math.Sqrt(12) +
               Math.Sqrt(13) + Math.Sqrt(14);
    }
}