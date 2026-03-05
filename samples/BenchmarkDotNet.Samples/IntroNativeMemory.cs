using System;
using System.Drawing;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Filters;

namespace BenchmarkDotNet.Samples
{
    [ShortRunJob]
    [NativeMemoryProfiler]
    [MemoryDiagnoser]
    public class IntroNativeMemory
    {
#pragma warning disable CA1416
        [Benchmark, WindowsOnly]
        public void BitmapWithLeaks()
        {
            var flag = new Bitmap(200, 100);
            var graphics = Graphics.FromImage(flag);
            var blackPen = new Pen(Color.Black, 3);
            graphics.DrawLine(blackPen, 100, 100, 500, 100);
        }

        [Benchmark, WindowsOnly]
        public void Bitmap()
        {
            using (var flag = new Bitmap(200, 100))
            {
                using (var graphics = Graphics.FromImage(flag))
                {
                    using (var blackPen = new Pen(Color.Black, 3))
                    {
                        graphics.DrawLine(blackPen, 100, 100, 500, 100);
                    }
                }
            }
        }
#pragma warning restore CA1416

        private const int Size = 20; // Greater value could cause System.OutOfMemoryException for test with memory leaks.
        private int ArraySize = Size * Marshal.SizeOf<int>();

        [Benchmark]
        public unsafe void AllocHGlobal()
        {
            IntPtr unmanagedHandle = Marshal.AllocHGlobal(ArraySize);
            Span<byte> unmanaged = new Span<byte>(unmanagedHandle.ToPointer(), ArraySize);
            Marshal.FreeHGlobal(unmanagedHandle);
        }

        [Benchmark]
        public unsafe void AllocHGlobalWithLeaks()
        {
            IntPtr unmanagedHandle = Marshal.AllocHGlobal(ArraySize);
            Span<byte> unmanaged = new Span<byte>(unmanagedHandle.ToPointer(), ArraySize);
        }

        private class WindowsOnlyAttribute : FilterConfigBaseAttribute
        {
            public WindowsOnlyAttribute()
                : base(new SimpleFilter(_ => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)))
            {
            }
        }
    }
}
