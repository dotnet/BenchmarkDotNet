using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Environments
{
    public class InfraResolver : Resolver
    {
        public static readonly IResolver Instance = new InfraResolver();

        private InfraResolver()
        {
            var infra = Job.Default.Infra;
            Register(infra.Clock, () => Chronometer.BestClock);
        }
    }
}