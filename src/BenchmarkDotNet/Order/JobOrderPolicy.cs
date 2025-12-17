namespace BenchmarkDotNet.Order;

public enum JobOrderPolicy
{
    /// <summary>
    /// Compare job characteristics in default order.
    /// </summary>
    Default,

    /// <summary>
    /// Compare job characteristics in numeric order.
    /// </summary>
    Numeric,

    /// <summary>
    /// Compare job characteristics in ordinal order.
    /// </summary>
    Ordinal,
}
