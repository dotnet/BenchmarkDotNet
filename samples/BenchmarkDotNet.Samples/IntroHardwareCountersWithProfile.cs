using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples
{
    /// <summary>
    /// Demonstration of benchmark setup using <see cref="HardwareCounter"/>, which cannot be automatically matched to your configuration.
    /// </summary>
    /// <remarks>
    /// Before starting, make sure the values from <see cref="HardwareCounterProfile"/> are available on your configuration.
    /// You can get a list of available counters using the command `tracelog.exe -profilesources Help`.
    /// </remarks>
    [HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.BranchInstructions)]
    public class IntroHardwareCountersWithProfile
    {
        /// <summary>
        /// The profile replaces the value from <see cref="HardwareCounter"/> with the one expected by your configuration.
        /// </summary>
        class HardwareCounterProfile : IHardwareCounterProfile
        {
            public IEnumerable<string> GetVariants(HardwareCounter hardwareCounter)
            {
                if (hardwareCounter == HardwareCounter.CacheMisses)
                {
                    // Example for `AMD Ryzen 7 5700G`
                    yield return "IcacheMisses";
                    yield return "DcacheMisses";
                }
                else
                {
                    yield return hardwareCounter.ToString();
                }
            }
        }

        public static void Run() =>
            BenchmarkRunner.Run<IntroHardwareCountersWithProfile>(DefaultConfig.Instance
                .AddJob(Job.Dry)
                .WithHardwareCounterProfile(new HardwareCounterProfile()));

        private const int N = 32767;
        private readonly int[] sorted, unsorted;

        public IntroHardwareCountersWithProfile()
        {
            var random = new Random(0);
            unsorted = new int[N];
            sorted = new int[N];
            for (int i = 0; i < N; i++)
                sorted[i] = unsorted[i] = random.Next(256);
            Array.Sort(sorted);
        }

        private static int Branch(int[] data)
        {
            int sum = 0;
            for (int i = 0; i < N; i++)
                if (data[i] >= 128)
                    sum += data[i];
            return sum;
        }

        private static int Branchless(int[] data)
        {
            int sum = 0;
            for (int i = 0; i < N; i++)
            {
                int t = (data[i] - 128) >> 31;
                sum += ~t & data[i];
            }
            return sum;
        }

        [Benchmark]
        public int SortedBranch() => Branch(sorted);

        [Benchmark]
        public int UnsortedBranch() => Branch(unsorted);

        [Benchmark]
        public int SortedBranchless() => Branchless(sorted);

        [Benchmark]
        public int UnsortedBranchless() => Branchless(unsorted);
    }
}