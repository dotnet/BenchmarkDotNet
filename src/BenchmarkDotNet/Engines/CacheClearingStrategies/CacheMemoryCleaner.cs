namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    /// <summary>
    /// This class clean CPU cache. To archive it, 
    /// </summary>
    internal class CacheMemoryCleaner : ICacheMemoryCleaner
    {
        private const int HowManyMb = 14;
        private const int HowManyRepeats = 10;
        private const int SizeOfArray = HowManyMb * 1024 * 1024;

        /// <summary>
        /// An array used to clean the cache. Thanks to the earlier allocation of this table, we save the allocation during cleaning.
        /// </summary>
        private readonly int[] memory = new int[SizeOfArray];

        /// <summary>
        /// This method performs read and write operations on the table every 64 bytes several times.
        /// You can find information about CPU cache in "Pro .NET Memory Management" Konrad Kokosa
        /// Chapter 2 "Low-level Memory Management" subtitle "CPU Cache" Page 77-96.
        /// </summary>
        public void Clean()
        {
            for (int i = 0; i < HowManyRepeats; i++)
            for (int j = 0; j < SizeOfArray; j += 16) // 16 because sizeof(int) = 4, 4 * 16 = 64 bytes.
                memory[j]++;
        }
    }
}