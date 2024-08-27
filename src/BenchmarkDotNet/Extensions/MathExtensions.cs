using System;

namespace BenchmarkDotNet.Extensions;

internal static class MathExtensions
{
    public static int RoundToInt(this double x) => (int)Math.Round(x);
    public static long RoundToLong(this double x) => (long)Math.Round(x);
}