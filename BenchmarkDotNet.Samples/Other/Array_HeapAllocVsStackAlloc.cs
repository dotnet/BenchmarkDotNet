namespace BenchmarkDotNet.Samples.Other
{
    // See http://blogs.microsoft.co.il/sasha/2013/10/17/on-stackalloc-performance-and-the-large-object-heap/
    // For comparision, the original "hand-written" benchmark is available at https://gist.github.com/goldshtn/7021608
    public class Array_HeapAllocVsStackAlloc
    {
        // Sizes used in original benchmark are 10 – 4010 (step 500) and 6000 – 96000 (step 10,000).
        [Params(10, 510, 1010, 1510, 2010, 2510, 3010, 3510, 4010, 6000, 16000, 26000, 36000, 46000, 56000, 66000, 76000, 86000, 96000)]
        public int ArraySize = 0;

        [Benchmark]
        public int GetSquare()
        {
            int value = ArraySize / 2;
            int[] someNumbers = new int[ArraySize];
            for (int i = 0; i < someNumbers.Length; ++i)
            {
                someNumbers[i] = value;
            }

            return someNumbers[value];
        }

        [Benchmark]
        public unsafe int GetSquareStack()
        {
            int value = ArraySize / 2;
            int* someNumbers = stackalloc int[ArraySize];
            for (int i = 0; i < ArraySize; ++i)
            {
                someNumbers[i] = value;
            }

            return someNumbers[value];
        }
    }
}
