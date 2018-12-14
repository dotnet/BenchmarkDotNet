using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    internal class MemoryAllocator : IMemoryAllocator
    {
        public void AllocateMemory()
        {
            const int howManyMb = 14;
            const int howManyPass = 10;
            const int sizeOfOneArray = 64;

            for (int i = 0; i < (howManyMb * 1024 * 1024 / sizeOfOneArray) * howManyPass; i++)
            {
                var tmpArray = new int[sizeOfOneArray];

                for (int j = 0; j < sizeOfOneArray; j++)
                {
                    tmpArray[j] = i * j;
                }

                tmpArray[0] = 6;

                for (int j = 0; j < sizeOfOneArray; j++)
                {
                    Consumer(tmpArray[j]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Consumer(int v)
        {

        }
    }
}