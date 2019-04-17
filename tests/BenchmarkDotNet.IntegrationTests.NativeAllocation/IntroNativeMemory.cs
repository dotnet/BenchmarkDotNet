using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.IntegrationTests.NativeWraper;

namespace BenchmarkDotNet.IntegrationTests.NativeAllocation
{
    [ShortRunJob]
    [NativeMemoryDiagnoser]
    [MemoryDiagnoser]
    public class IntroNativeMemory
    {
        private const int Size = 10000;

        [Benchmark]
        public int AllocIntArray() => SumMemory<int>(
            () => (NativeDll.AllocateArrayOfInt(Size), Size),
            (i => i));

        [Benchmark]
        public int AllocAndFreeIntArray() => SumMemory<int>(
            () => (NativeDll.AllocateArrayOfInt(Size), Size),
            (i => i),
            ptr => NativeDll.DeallocateArrayOfInt(ptr));

        [Benchmark]
        public int AllocStructArray() => SumMemory<Point>(
            () => (NativeDll.AllocateArrayOfPoint(Size), Size),
            (i => i.X + i.Y));

        [Benchmark]
        public int AllocAndFreeStructArray() => SumMemory<Point>(
            () => (NativeDll.AllocateArrayOfPoint(Size), Size),
            (i => i.X + i.Y),
            ptr => NativeDll.DeallocateArrayOfPoint(ptr));

        [Benchmark]
        public int MarshalAllocIntArray() => SumMemory<int>(
            () => (Marshal.AllocHGlobal(Size * Marshal.SizeOf(typeof(int))), Size),
            (i => i));

        [Benchmark]
        public int MarshalAllocAndFreeIntArray() => SumMemory<int>(
            () => (Marshal.AllocHGlobal(Size * Marshal.SizeOf(typeof(int))), Size),
            (i => i),
            ptr => Marshal.FreeHGlobal(ptr));

        [Benchmark]
        public int MarshalAllocStructArray() => SumMemory<Point>(
            () => (Marshal.AllocHGlobal(Size * Marshal.SizeOf(typeof(Point))), Size),
            (i => i.X + i.Y));

        [Benchmark]
        public int MarshalAllocAndFreeStructArray() => SumMemory<Point>(
            () => (Marshal.AllocHGlobal(Size * Marshal.SizeOf(typeof(Point))), Size),
            (i => i.X + i.Y),
            ptr => Marshal.FreeHGlobal(ptr));

        
        [Benchmark]
        public int AllocAndFreeManyType()
        {
            var result = 0;
            result += AllocIntArray();
            result += AllocAndFreeIntArray();
            result += AllocStructArray();
            result += AllocAndFreeStructArray();
            return result;
        }

        [Benchmark]
        public int ManagedAlloc()
        {
            var array = new int[Size];
            for (int i = 0; i < Size; i++)
            {
                array[i] = i;
            }

            int result = 0;
            
            for (int i = 0; i < Size; i++)
            {
                result += array[i];
            }

            return result;
        }

        public int SumMemory<TType>(Func<(IntPtr ptr, int size)> allocate, Func<TType, int> sumFunc, Action<IntPtr> deallocate=null) where TType : unmanaged
        {
            int result = 0;
            var allocated = allocate();
            unsafe
            {
                var bytes = new Span<TType>((TType*)allocated.ptr, allocated.size);
                foreach (var item in bytes)
                {
                    result += sumFunc(item);
                }
            }

            deallocate?.Invoke(allocated.ptr);

            return result;
        }
    }
}