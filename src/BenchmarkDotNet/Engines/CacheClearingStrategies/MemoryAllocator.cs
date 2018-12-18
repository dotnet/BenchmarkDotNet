using System.Linq;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    internal class MemoryAllocator : IMemoryAllocator
    {
        private const int HowManyMb = 14;
        private const int SizeOfArray = HowManyMb * 1024 * 1024;
        private readonly int[] memory = Enumerable.Range(1, SizeOfArray).ToArray();

        public void AllocateMemory()
        {
            for (int i = 0; i < SizeOfArray; i++)
            {
                Consumer(memory[i]);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Consumer(int v)
        {

        }
    }
}