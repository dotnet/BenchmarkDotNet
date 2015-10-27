using System;

namespace BenchmarkDotNet.Samples.Algorithms
{
    // See http://en.wikipedia.org/wiki/Hamming_weight
    public class Algo_BitCount
    {
        private const int N = 1002;
        private readonly ulong[] numbers;
        private readonly Random random = new Random(42);

        public ulong NextUInt64()
        {
            var buffer = new byte[sizeof(long)];
            random.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }

        public Algo_BitCount()
        {
            numbers = new ulong[N];
            for (int i = 0; i < N; i++)
                numbers[i] = NextUInt64();
        }

        [Benchmark]
        public int PopCount1()
        {
            int counter = 0;
            for (int i = 0; i < N; i++)
                counter += BitCountHelper.PopCount1(numbers[i]);
            return counter;
        }

        [Benchmark]
        public int PopCount2()
        {
            int counter = 0;
            for (int i = 0; i < N; i++)
                counter += BitCountHelper.PopCount2(numbers[i]);
            return counter;
        }

        [Benchmark]
        public int PopCount3()
        {
            int counter = 0;
            for (int i = 0; i < N; i++)
                counter += BitCountHelper.PopCount3(numbers[i]);
            return counter;
        }

        [Benchmark]
        public int PopCount4()
        {
            int counter = 0;
            for (int i = 0; i < N; i++)
                counter += BitCountHelper.PopCount4(numbers[i]);
            return counter;
        }

        [Benchmark]
        public int PopCountParallel2()
        {
            int counter = 0;
            for (int i = 0; i < N / 2; i++)
                counter += BitCountHelper.PopCountParallel2(numbers[i],numbers[i+1]);
            return counter;
        }
    }

    /// <summary>
    /// The original code was taken from Wikipedia
    /// http://en.wikipedia.org/wiki/Hamming_weight
    /// </summary>
    internal static class BitCountHelper
    {
        const ulong m1 = 0x5555555555555555;
        const ulong m2 = 0x3333333333333333;
        const ulong m4 = 0x0f0f0f0f0f0f0f0f;
        const ulong m8 = 0x00ff00ff00ff00ff;
        const ulong m16 = 0x0000ffff0000ffff;
        const ulong m32 = 0x00000000ffffffff;
        const ulong hff = 0xffffffffffffffff;
        const ulong h01 = 0x0101010101010101;

        //This is a naive implementation, shown for comparison,
        //and to help in understanding the better functions.
        //It uses 24 arithmetic operations (shift, add, and).
        public static int PopCount1(ulong x)
        {
            x = (x & m1) + ((x >> 1) & m1); //put count of each  2 bits into those  2 bits 
            x = (x & m2) + ((x >> 2) & m2); //put count of each  4 bits into those  4 bits 
            x = (x & m4) + ((x >> 4) & m4); //put count of each  8 bits into those  8 bits 
            x = (x & m8) + ((x >> 8) & m8); //put count of each 16 bits into those 16 bits 
            x = (x & m16) + ((x >> 16) & m16); //put count of each 32 bits into those 32 bits 
            x = (x & m32) + ((x >> 32) & m32); //put count of each 64 bits into those 64 bits 
            return (int)x;
        }

        //This uses fewer arithmetic operations than any other known  
        //implementation on machines with slow multiplication.
        //It uses 17 arithmetic operations.
        public static int PopCount2(ulong x)
        {
            x -= (x >> 1) & m1;             //put count of each 2 bits into those 2 bits
            x = (x & m2) + ((x >> 2) & m2); //put count of each 4 bits into those 4 bits 
            x = (x + (x >> 4)) & m4;        //put count of each 8 bits into those 8 bits 
            x += x >> 8;  //put count of each 16 bits into their lowest 8 bits
            x += x >> 16;  //put count of each 32 bits into their lowest 8 bits
            x += x >> 32;  //put count of each 64 bits into their lowest 8 bits
            return (int)x;
        }

        //This uses fewer arithmetic operations than any other known  
        //implementation on machines with fast multiplication.
        //It uses 12 arithmetic operations, one of which is a multiply.
        public static int PopCount3(ulong x)
        {
            x -= (x >> 1) & m1;             //put count of each 2 bits into those 2 bits
            x = (x & m2) + ((x >> 2) & m2); //put count of each 4 bits into those 4 bits 
            x = (x + (x >> 4)) & m4;        //put count of each 8 bits into those 8 bits 
            return (int)((x * h01) >> 56);  //returns left 8 bits of x + (x<<8) + (x<<16) + (x<<24) + ... 
        }

        // This implementation uses branches to count. 
        // Found at https://github.com/aspnet/KestrelHttpServer/blob/78de14d24868fd1b44a7335b30d9a2064c984516/src/Microsoft.AspNet.Server.Kestrel/Http/FrameHeaders.Generated.cs#L65
        public static int PopCount4(ulong x)
        {
            int count = 0;

            if (((x & 1L) != 0))
            {
                ++count;
            }

            if (((x & 2L) != 0))
            {
                ++count;
            }

            if (((x & 4L) != 0))
            {
                ++count;
            }

            if (((x & 8L) != 0))
            {
                ++count;
            }

            if (((x & 16L) != 0))
            {
                ++count;
            }

            if (((x & 32L) != 0))
            {
                ++count;
            }

            if (((x & 64L) != 0))
            {
                ++count;
            }

            if (((x & 128L) != 0))
            {
                ++count;
            }

            if (((x & 256L) != 0))
            {
                ++count;
            }

            if (((x & 512L) != 0))
            {
                ++count;
            }

            if (((x & 1024L) != 0))
            {
                ++count;
            }

            if (((x & 2048L) != 0))
            {
                ++count;
            }

            if (((x & 4096L) != 0))
            {
                ++count;
            }

            if (((x & 8192L) != 0))
            {
                ++count;
            }

            if (((x & 16384L) != 0))
            {
                ++count;
            }

            if (((x & 32768L) != 0))
            {
                ++count;
            }

            if (((x & 65536L) != 0))
            {
                ++count;
            }

            if (((x & 131072L) != 0))
            {
                ++count;
            }

            if (((x & 262144L) != 0))
            {
                ++count;
            }

            if (((x & 524288L) != 0))
            {
                ++count;
            }

            if (((x & 1048576L) != 0))
            {
                ++count;
            }

            if (((x & 2097152L) != 0))
            {
                ++count;
            }

            if (((x & 4194304L) != 0))
            {
                ++count;
            }

            if (((x & 8388608L) != 0))
            {
                ++count;
            }

            if (((x & 16777216L) != 0))
            {
                ++count;
            }

            if (((x & 33554432L) != 0))
            {
                ++count;
            }

            if (((x & 67108864L) != 0))
            {
                ++count;
            }

            if (((x & 134217728L) != 0))
            {
                ++count;
            }

            if (((x & 268435456L) != 0))
            {
                ++count;
            }

            if (((x & 536870912L) != 0))
            {
                ++count;
            }

            if (((x & 1073741824L) != 0))
            {
                ++count;
            }

            if (((x & 2147483648L) != 0))
            {
                ++count;
            }

            if (((x & 4294967296L) != 0))
            {
                ++count;
            }

            if (((x & 8589934592L) != 0))
            {
                ++count;
            }

            if (((x & 17179869184L) != 0))
            {
                ++count;
            }

            if (((x & 34359738368L) != 0))
            {
                ++count;
            }

            if (((x & 68719476736L) != 0))
            {
                ++count;
            }

            if (((x & 137438953472L) != 0))
            {
                ++count;
            }

            if (((x & 274877906944L) != 0))
            {
                ++count;
            }

            if (((x & 549755813888L) != 0))
            {
                ++count;
            }

            if (((x & 1099511627776L) != 0))
            {
                ++count;
            }

            return count;
        }

        public static int PopCountParallel2(ulong x, ulong y)
        {
            x -= (x >> 1) & m1;             //put count of each 2 bits into those 2 bits
            y -= (y >> 1) & m1;             //put count of each 2 bits into those 2 bits

            x = (x & m2) + ((x >> 2) & m2); //put count of each 4 bits into those 4 bits 
            y = (y & m2) + ((y >> 2) & m2); //put count of each 4 bits into those 4 bits 

            x = (x + (x >> 4)) & m4;        //put count of each 8 bits into those 8 bits                         
            y = (y + (y >> 4)) & m4;        //put count of each 8 bits into those 8 bits 

            return (int) ( ((y * h01) >> 56) + ((x * h01) >> 56) );  //returns left 8 bits of x + (x<<8) + (x<<16) + (x<<24) + ... 
        }
    }
}