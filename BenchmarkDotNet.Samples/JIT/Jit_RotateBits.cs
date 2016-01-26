using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.JIT
{
    public class Jit_RotateBits
    {
        private ulong au = 2340988;
        private ulong bu = 123444;
        private ulong cu = 1;
        private ulong du = 23444111111;

        private long ax = 2340988;
        private long bx = 123444;
        private long cx = 1;
        private long dx = 23444111111;

        [Benchmark]
        [OperationsPerInvoke(4)]
        public void ShouldOptimize()
        {
            RotateRight64(au, 16);
            RotateRight64(bu, 24);
            RotateRight64(cu, 32);
            RotateRight64(du, 48);
        }

        [Benchmark]
        [OperationsPerInvoke(4)]
        public void ShouldNotOptimize()
        {
            RotateRight64(ax, 16);
            RotateRight64(bx, 24);
            RotateRight64(cx, 32);
            RotateRight64(dx, 48);
        }

        public static ulong RotateRight64(ulong value, int count)
        {
            return (value >> count) | (value << (64 - count));
        }

        /// This case should not be optimized because signed Right-Shift have special treatment for the negative case.
        /// https://github.com/dotnet/coreclr/issues/1619#issuecomment-151609929
        public static long RotateRight64(long value, int count)
        {
            return (value >> count) | (value << (64 - count));
        }
    }
}
