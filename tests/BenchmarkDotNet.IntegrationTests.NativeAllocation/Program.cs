using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.IntegrationTests.NativeWraper;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.IntegrationTests.NativeAllocation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            
//            var obj = new IntroNativeMemory();
//            Console.WriteLine("AllocateIntArray " + obj.AllocateIntArray());
//            Console.WriteLine("AllocateAndDeallocateIntArray " + obj.AllocateAndDeallocateIntArray());
//            Console.WriteLine("AllocateStructArray " + obj.AllocateStructArray());
//            Console.WriteLine("AllocateAndDeallocateStructArray " + obj.AllocateAndDeallocateStructArray());
        }
    }

    [ShortRunJob]
    [NativeMemoryDiagnoser]
    [MemoryDiagnoser]
    public class IntroNativeMemory
    {
        [Benchmark]
        public int AllocateIntArray() => Allocate(10000);

        [Benchmark]
        public int AllocateAndDeallocateIntArray() => AllocateAndDeallocate(10000);

        [Benchmark]
        public int AllocateStructArray() => AllocateStruct(10000);

        [Benchmark]
        public int AllocateAndDeallocateStructArray() => AllocateAndDeallocateStruct(10000);

        [Benchmark]
        public int AllocateAndDeallocateManyType()
        {
            var result = 0;
            result += Allocate(10000);
            result += AllocateAndDeallocate(10000);
            result += AllocateStruct(10000);
            result += AllocateAndDeallocateStruct(10000);
            return result;
        }

        [Benchmark]
        public int ManagedAllocation()
        {
            var array = new int[10000];
            for (int i = 0; i < 10000; i++)
            {
                array[i] = i;
            }

            int result = 0;
            for (int i = 0; i < 10000; i++)
            {
                result += array[i];
            }

            return result;
        }

        static int AllocateStruct(int size)
        {
            int i = 0;
            var ptr = NativeDll.AllocateArrayOfPoint(size);
            Span<Point> bytes;
            unsafe
            {
                bytes = new Span<Point>((Point*)ptr, size);
                foreach (Point item in bytes)
                {
                    i += item.X + item.Y;
                }
            }
            return i;

        }

        static int AllocateAndDeallocateStruct(int size)
        {
            int i = 0;
            var ptr = NativeDll.AllocateArrayOfPoint(size);
            Span<Point> bytes;
            unsafe
            {
                bytes = new Span<Point>((Point*)ptr, size);
                foreach (Point item in bytes)
                {
                    i += item.X + item.Y;
                }
            }
            NativeDll.DeallocateArrayOfPoint(ptr);
            return i;

        }

        static int Allocate(int size)
        {
            int i = 0;
            var ptr = NativeDll.AllocateArrayOfInt(size);
            Span<int> bytes;
            unsafe
            {
                bytes = new Span<int>((int*)ptr, size);
                foreach (int item in bytes)
                {
                    i += item;
                }
            }
            return i;

        }

        static int AllocateAndDeallocate(int size)
        {
            int i = 0;
            var ptr = NativeDll.AllocateArrayOfInt(size);
            Span<int> bytes;
            unsafe
            {
                bytes = new Span<int>((int*)ptr, size);
                foreach (int item in bytes)
                {
                    i += item;
                }
            }
            NativeDll.DeallocateArrayOfInt(ptr);
            return i;

        }
    }
}
