using System;

namespace BenchmarkDotNet.Columns
{
    public enum UnitType
    {
        Dimensionless,
        Time,
        [Obsolete("Use " + nameof(Allocation))]
        Size,
#pragma warning disable CS0618
        Allocation = Size,
#pragma warning restore CS0618
        CodeSize
    }
}
