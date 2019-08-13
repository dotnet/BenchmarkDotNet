using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples
{
    [DryJob(TargetFrameworkMoniker.NetCoreApp21)]
    [DryJob(TargetFrameworkMoniker.Mono)]
    [DryJob(TargetFrameworkMoniker.Net461, Jit.LegacyJit, Platform.X86)]
    [DisassemblyDiagnoser]
    public class IntroDisassembly
    {
        [Benchmark]
        public double Sum()
        {
            double res = 0;
            for (int i = 0; i < 64; i++)
                res += i;
            return res;
        }
    }
}