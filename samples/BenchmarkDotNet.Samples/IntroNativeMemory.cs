using System.Drawing;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;

namespace BenchmarkDotNet.Samples
{
    [ShortRunJob]
    [NativeMemoryDiagnoser]
    [MemoryDiagnoser]
    public class IntroNativeMemory
    {
        [Benchmark]
        public void BitmapWithLeaks()
        {
            var flag = new Bitmap(200, 100);
            var graphics = Graphics.FromImage(flag);
            var blackPen = new Pen(Color.Black, 3);
            graphics.DrawLine(blackPen, 100, 100, 500, 100);
        }

        [Benchmark]
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
    }
}
