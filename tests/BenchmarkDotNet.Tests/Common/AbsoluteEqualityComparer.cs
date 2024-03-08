using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Tests.Common;

public class AbsoluteEqualityComparer(double eps) : IEqualityComparer<double>
{
    public static readonly IEqualityComparer<double> E4 = new AbsoluteEqualityComparer(1e-4);
    public static readonly IEqualityComparer<double> E5 = new AbsoluteEqualityComparer(1e-5);
    public static readonly IEqualityComparer<double> E9 = new AbsoluteEqualityComparer(1e-9);

    public bool Equals(double x, double y)
    {
        if (double.IsPositiveInfinity(x) && double.IsPositiveInfinity(y))
            return true;
        if (double.IsNegativeInfinity(x) && double.IsNegativeInfinity(y))
            return true;
        if (double.IsNaN(x) && double.IsNaN(y))
            return true;
        return Math.Abs(x - y) < eps;
    }

    public int GetHashCode(double x) => x.GetHashCode();
}