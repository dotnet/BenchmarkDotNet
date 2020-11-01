using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class IntroSetupCleanupGlobal
    {
        [Params(10, 100, 1000)]
        public int N;

        private int[] data;

        [GlobalSetup]
        public void GlobalSetup()
        {
            data = new int[N]; // executed once per each N value
        }

        [Benchmark]
        public int Logic()
        {
            int res = 0;
            for (int i = 0; i < N; i++)
                res += data[i];
            return res;
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            // Disposing logic
        }
    }
}