using System;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
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
    }

    public class Algo_BitCount
    {
        private const int IterationCount = 100001;
        private const int ArrayLength = 1001;
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
            numbers = new ulong[ArrayLength];
            for (int i = 0; i < ArrayLength; i++)
                numbers[i] = NextUInt64();
        }

        [Benchmark]
        public int PopCount1()
        {
            int counter = 0;
            for (int i = 0; i < IterationCount; i++)
                for (int j = 0; j < ArrayLength; j++)
                    counter += BitCountHelper.PopCount1(numbers[j]);
            return counter;
        }

        [Benchmark]
        public int PopCount2()
        {
            int counter = 0;
            for (int i = 0; i < IterationCount; i++)
                for (int j = 0; j < ArrayLength; j++)
                    counter += BitCountHelper.PopCount2(numbers[j]);
            return counter;
        }

        [Benchmark]
        public int PopCount3()
        {
            int counter = 0;
            for (int i = 0; i < IterationCount; i++)
                for (int j = 0; j < ArrayLength; j++)
                    counter += BitCountHelper.PopCount3(numbers[j]);
            return counter;
        }
    }
}