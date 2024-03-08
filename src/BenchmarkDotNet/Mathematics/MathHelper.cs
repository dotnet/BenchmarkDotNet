using Perfolizer.Mathematics.Common;
using Perfolizer.Metrology;
using static System.Math;

namespace BenchmarkDotNet.Mathematics
{
    internal static class MathHelper
    {
        public static readonly Threshold DefaultThreshold = PercentValue.Of(2).ToThreshold();
        public static readonly SignificanceLevel DefaultSignificanceLevel = SignificanceLevel.P1E5;
        public static int Clamp(int value, int min, int max) => Min(Max(value, min), max);
    }
}