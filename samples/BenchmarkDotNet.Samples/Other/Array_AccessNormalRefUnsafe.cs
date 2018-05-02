using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.Samples.Other
{
    // See: http://stackoverflow.com/q/8497018/974487
    [LegacyJitX86Job, LegacyJitX64Job, RyuJitX64Job]
    public class Array_AccessNormalRefUnsafe
    {
        private const int Iterations = 111;
        private static float[] buffer = new float[1024 * 1024 * 100];
        private static readonly int Len = buffer.Length;

        [Benchmark]
        public void NormalAccess()
        {
            for (var i = 0; i < Iterations; i++)
            {
                Test.NormalAccess(buffer, i % Len);
            }
        }

        [Benchmark]
        public void NormalRefAccess()
        {
            for (var i = 0; i < Iterations; i++)
            {
                Test.NormalRefAccess(ref buffer, i % Len);
            }
        }

        [Benchmark]
        public void IntPtrAccessFixedOutsideLoop()
        {
            unsafe
            {
                fixed (float* ptr = &buffer[0])
                {
                    for (var i = 0; i < Iterations; i++)
                    {
                        Test.IntPtrAccess((IntPtr)ptr, i % Len);
                    }
                }
            }
        }

        [Benchmark]
        public void IntPtrMisalignedAccessFixedOutsideLoop()
        {
            unsafe
            {
                fixed (float* ptr = &buffer[0])
                {
                    for (var i = 0; i < Iterations; i++)
                    {
                        Test.IntPtrMisalignedAccess((IntPtr)ptr, i % Len);
                    }
                }
            }
        }

        [Benchmark]
        public void FixedAccessFixedInsideLoop()
        {
            for (var i = 0; i < Iterations; i++)
            {
                Test.FixedAccess(buffer, i % Len);
            }
        }

        [Benchmark]
        public void PtrAccessFixedOutsideLoop()
        {
            unsafe
            {
                fixed (float* ptr = &buffer[0])
                {
                    for (var i = 0; i < Iterations; i++)
                    {
                        Test.PtrAccess(ptr + (i % Len));
                    }
                }
            }
        }

        [Benchmark]
        public void PtrAccessFixedInsideLoop()
        {
            unsafe
            {
                for (var i = 0; i < Iterations; i++)
                {
                    fixed (float* ptr = &buffer[i % Len])
                    {
                        Test.PtrAccess(ptr);
                    }
                }
            }
        }
    }

    public class Test
    {
        public static void NormalAccess(float[] array, int index)
        {
            array[index] = array[index] + 2;
        }

        public static void NormalRefAccess(ref float[] array, int index)
        {
            array[index] = array[index] + 2;
        }

        public static void IntPtrAccess(IntPtr arrayPtr, int index)
        {
            unsafe
            {
                var array = (float*)IntPtr.Add(arrayPtr, index << 2);
                (*array) = (*array) + 2;
            }
        }

        public static void IntPtrMisalignedAccess(IntPtr arrayPtr, int index)
        {
            unsafe
            {
                var array = (float*)IntPtr.Add(arrayPtr, index); // getting bits of a float
                (*array) = (*array) + 2;
            }
        }

        public static void FixedAccess(float[] array, int index)
        {
            unsafe
            {
                fixed (float* ptr = &array[index])
                {
                    (*ptr) = (*ptr) + 2;
                }
            }
        }

        public static unsafe void PtrAccess(float* ptr)
        {
            (*ptr) = (*ptr) + 2;
        }
    }
}