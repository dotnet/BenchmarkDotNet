using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using System.Diagnostics;

namespace BenchmarkDotNet.Samples.JIT
{
    [Config(typeof(CustomPathsConfig))]
    [DisassemblyDiagnoser]
    public class Jit_RyuJitX86
    {
        public class CustomPathsConfig : ManualConfig
        {
            public CustomPathsConfig() 
            {
                var dotnetCli32bit = NetCoreAppSettings
                    .NetCoreApp20
                    .WithCustomDotNetCliPath(@"C:\Program Files (x86)\dotnet\dotnet.exe", "32 bit cli");

                var dotnetCli64bit = NetCoreAppSettings
                    .NetCoreApp20
                    .WithCustomDotNetCliPath(@"C:\Program Files\dotnet\dotnet.exe", "64 bit cli");

                Add(Job.RyuJitX86.With(CsProjCoreToolchain.From(dotnetCli32bit)).WithId("32 bit cli"));
                Add(Job.RyuJitX64.With(CsProjCoreToolchain.From(dotnetCli64bit)).WithId("64 bit cli"));
            }
        }

        [Params(false, true)]
        public bool CallStopwatchTimestamp { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            if (CallStopwatchTimestamp)
                Stopwatch.GetTimestamp();
        }

        private const int IterationCount = 10001;

        [Benchmark(OperationsPerInvoke = IterationCount)]
        public string WithStopwatch()
        {
            double a = 1, b = 1;
            var sw = new Stopwatch();
            for (int i = 0; i < IterationCount; i++)
            {
                // fld1
                // fadd        qword ptr [ebp-0Ch]
                // fstp        qword ptr [ebp-0Ch]
                a = a + b;
            }
            return string.Format("{0}{1}", a, sw.ElapsedMilliseconds);
        }

        [Benchmark(OperationsPerInvoke = IterationCount)]
        public string WithoutStopwatch()
        {
            double a = 1, b = 1;
            for (int i = 0; i < IterationCount; i++)
            {
                // fld1
                // faddp       st(1),st
                a = a + b;
            }
            return string.Format("{0}", a);
        }
    }
}
