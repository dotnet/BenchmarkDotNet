using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.Samples.Algorithms
{
    [LegacyJitX64Job, RyuJitX64Job]
    public class Algo_MostSignificantBit
    {
        private const int N = 4001;
        private readonly int[] numbers;
        private readonly Random random = new Random(42);

        public Algo_MostSignificantBit()
        {
            numbers = new int[N];
            for (int i = 0; i < N; i++)
                numbers[i] = random.Next();
        }

        [Benchmark]
        public int DeBruijn()
        {
            int counter = 0;
            for (int i = 0; i < N; i++)
                counter += BitHelper.MostSignificantDeBruijn(numbers[i]);
            return counter;
        }

        [Benchmark]
        public int Naive()
        {
            int counter = 0;
            for (int i = 0; i < N; i++)
                counter += BitHelper.MostSignificantNaive(numbers[i]);
            return counter;
        }

        [Benchmark]
        public int Shifted()
        {
            int counter = 0;
            for (int i = 0; i < N; i++)
                counter += BitHelper.MostSignificantShifted(numbers[i]);
            return counter;
        }

        [Benchmark]
        public int Branched()
        {
            int counter = 0;
            for (int i = 0; i < N; i++)
                counter += BitHelper.MostSignificantBranched(numbers[i]);
            return counter;
        }

        public static class BitHelper
        {
            // Code taken from http://graphics.stanford.edu/~seander/bithacks.html#IntegerLogDeBruijn

            private static readonly int[] MultiplyDeBruijnBitPosition = new int[]
                {
                    0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
                    8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
                };

            public static int MostSignificantDeBruijn(int v)
            {
                v |= v >> 1; // first round down to one less than a power of 2
                v |= v >> 2;
                v |= v >> 4;
                v |= v >> 8;
                v |= v >> 16;

                return MultiplyDeBruijnBitPosition[(uint)(v * 0x07C4ACDDU) >> 27];
            }

            public static int MostSignificantNaive(int n)
            {
                int r = 0;

                n >>= 1;
                while (n != 0)
                {
                    r++;
                    n >>= 1;
                }

                return r;
            }

            private static readonly int[] BranchedValues = new int[] { 0, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4 };

            public static int MostSignificantBranched(int x)
            {
                if (x == 0)
                    return 0;

                int r = 0;
                if ((x & 0xFFFF0000) != 0) { r += 16 / 1; x >>= 16 / 1; }
                if ((x & 0x0000FF00) != 0) { r += 16 / 2; x >>= 16 / 2; }
                if ((x & 0x000000F0) != 0) { r += 16 / 4; x >>= 16 / 4; }
                return r + BranchedValues[x] - 1;
            }


            public static int MostSignificantShifted(int n)
            {
                uint v = (uint)n;           // 32-bit value to find the log2 of
                uint r;                     // result of log2(v) will go here
                uint shift;

                unsafe
                {
                    bool cond = v > 0xFFFF;
                    r = *(uint*)(&cond) << 4;

                    v >>= (int)r;

                    cond = v > 0xFF;
                    shift = *(uint*)(&cond) << 3; v >>= (int)shift; r |= shift;

                    cond = v > 0xF;
                    shift = *(uint*)(&cond) << 2; v >>= (int)shift; r |= shift;

                    cond = v > 0x3;
                    shift = *(uint*)(&cond) << 1; v >>= (int)shift; r |= shift;

                    r |= (v >> 1);

                    return (int)r;
                }
            }
        }
    }
}
