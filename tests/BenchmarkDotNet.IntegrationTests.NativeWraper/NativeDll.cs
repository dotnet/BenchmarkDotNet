using System;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.IntegrationTests.NativeWraper
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;
    }

    public class NativeDll
    {
        [DllImport("BenchmarkDotNet.IntegrationTests.Native.dll")]
        public static extern IntPtr AllocateArrayOfInt(int count);

        [DllImport("BenchmarkDotNet.IntegrationTests.Native.dll")]
        public static extern void DeallocateArrayOfInt(IntPtr ptr);


        [DllImport("BenchmarkDotNet.IntegrationTests.Native.dll")]
        public static extern IntPtr AllocateArrayOfPoint(int count);

        [DllImport("BenchmarkDotNet.IntegrationTests.Native.dll")]
        public static extern void DeallocateArrayOfPoint(IntPtr ptr);
    }
}
