using static System.Math;

namespace BenchmarkDotNet.Mathematics
{
    public static class MathHelper
    {

        public static int Clamp(int value, int min, int max) => Min(Max(value, min), max);
    }
}