using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples
{
    [HardwareCounters(HardwareCounter.CacheMisses)]
    public class IntroHardwareCountersCustom
    {
        public static void Run() =>
            _ = BenchmarkRunner.Run<IntroHardwareCountersCustom>(DefaultConfig.Default
                .AddHardwareCounterProvider(CustomHardwareCounterProvider.Instance));

        private const int N = 32767;
        private readonly int[] sorted, unsorted;

        public IntroHardwareCountersCustom()
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

public class CustomHardwareCounterProvider : IHardwareCounterProvider
{
    public static readonly IHardwareCounterProvider Instance = new CustomHardwareCounterProvider();

    public IEnumerable<string> GetVariants(HardwareCounter hardwareCounter)
    {
        switch (hardwareCounter)
        {
            case HardwareCounter.CacheMisses:
                yield return "cache-references";
                yield return "cache-misses";
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(hardwareCounter), hardwareCounter, null);
        }
    }
}