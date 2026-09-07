namespace BenchmarkDotNet.Helpers
{
    /// <summary>
    /// A type that was considered for running benchmarks, and the reason it cannot be run when it cannot.
    /// </summary>
    internal readonly struct GenericBenchmarkType
    {
        private GenericBenchmarkType(Type type, string? error)
        {
            Type = type;
            Error = error;
        }

        /// <summary>
        /// Gets the type. It is the type the benchmarks are read from when <see cref="IsSuccess"/>, and the type that
        /// was rejected otherwise.
        /// </summary>
        internal Type Type { get; }

        /// <summary>
        /// Gets the reason the type cannot be run, or null when it can.
        /// </summary>
        internal string? Error { get; }

        internal bool IsSuccess => Error == null;

        internal static GenericBenchmarkType Runnable(Type type) => new GenericBenchmarkType(type, null);

        internal static GenericBenchmarkType Failed(Type type, string error) => new GenericBenchmarkType(type, error);
    }
}
