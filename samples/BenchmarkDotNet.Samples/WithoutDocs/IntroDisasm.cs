using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Samples
{
    [DryCoreJob, DryMonoJob, DryClrJob(Platform.X86)]
    [DisassemblyDiagnoser]
    public class IntroDisasm
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