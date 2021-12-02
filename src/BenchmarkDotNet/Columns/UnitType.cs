using System;

namespace BenchmarkDotNet.Columns
{
    public enum UnitType
    {
        Dimensionless,
        Time,
        Allocation,
        [Obsolete("Use " + nameof(Allocation))]
        Size = Allocation,
        CodeSize
    }
}
