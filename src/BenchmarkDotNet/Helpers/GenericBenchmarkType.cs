namespace BenchmarkDotNet.Helpers
{
    /// <summary>
    /// A type that was considered for running benchmarks, and the reason it cannot be run when it cannot.
    /// </summary>
    internal readonly struct GenericBenchmarkType
    {
        private GenericBenchmarkType(Type type, string? error, bool isUnreadable)
        {
            Type = type;
            Error = error;
            IsUnreadable = isUnreadable;
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

        /// <summary>
        /// Gets whether the type was rejected before any of its benchmarks could be read, rather than because one set
        /// of [GenericTypeArguments] did not fit it. Nothing downstream reports such a type - GenericBenchmarksValidator
        /// only runs once at least one benchmark of the assembly survived - so whoever drops it has to say so.
        /// </summary>
        internal bool IsUnreadable { get; }

        internal bool IsSuccess => Error == null;

        internal static GenericBenchmarkType Runnable(Type type) => new GenericBenchmarkType(type, null, false);

        internal static GenericBenchmarkType Failed(Type type, string error) => new GenericBenchmarkType(type, error, false);

        internal static GenericBenchmarkType Unreadable(Type type, string error) => new GenericBenchmarkType(type, error, true);
    }
}
