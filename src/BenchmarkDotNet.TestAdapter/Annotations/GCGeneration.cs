namespace BenchmarkDotNet.TestAdapter.Annotations
{
    /// <summary>
    /// GC Generation
    /// </summary>
    public enum GCGeneration: int
    {
        /// <summary>
        /// GC Generation 0
        /// </summary>
        Gen0,
        /// <summary>
        /// GC Generation 1
        /// </summary>
        Gen1,
        /// <summary>
        /// GC Generation 2
        /// </summary>
        Gen2
    }
}
