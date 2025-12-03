using System;
using System.Threading;

#nullable enable

namespace BenchmarkDotNet.Engines
{
    public struct ThreadingStats : IEquatable<ThreadingStats>
    {
        internal const string ResultsLinePrefix = "// Threading: ";

#if NETSTANDARD2_0
        // BDN targets .NET Standard 2.0, these properties are not part of .NET Standard 2.0, were added in .NET Core 3.0
        private static readonly Func<long> GetCompletedWorkItemCountDelegate = CreateGetterDelegate(typeof(ThreadPool), nameof(CompletedWorkItemCount));
        private static readonly Func<long> GetLockContentionCountDelegate = CreateGetterDelegate(typeof(Monitor), nameof(LockContentionCount));
#endif
        public static ThreadingStats Empty => new ThreadingStats(0, 0, 0);

        public long CompletedWorkItemCount { get; }
        public long LockContentionCount { get; }
        public long TotalOperations { get; }

        public ThreadingStats(long completedWorkItemCount, long lockContentionCount, long totalOperations)
        {
            CompletedWorkItemCount = completedWorkItemCount;
            LockContentionCount = lockContentionCount;
            TotalOperations = totalOperations;
        }

        public static ThreadingStats ReadInitial()
        {
#if NETSTANDARD2_0
            long lockContentionCount = GetLockContentionCountDelegate(); // Monitor.LockContentionCount can schedule a work item and needs to be called before ThreadPool.CompletedWorkItemCount
            long completedWorkItemCount = GetCompletedWorkItemCountDelegate();
#else
            long lockContentionCount = Monitor.LockContentionCount;
            long completedWorkItemCount = ThreadPool.CompletedWorkItemCount;
#endif

            return new ThreadingStats(completedWorkItemCount, lockContentionCount, 0);
        }

        public static ThreadingStats ReadFinal()
        {
#if NETSTANDARD2_0
            long lockContentionCount = GetLockContentionCountDelegate(); // Monitor.LockContentionCount can schedule a work item and needs to be called before ThreadPool.CompletedWorkItemCount
            long completedWorkItemCount = GetCompletedWorkItemCountDelegate();
#else
            long lockContentionCount = Monitor.LockContentionCount;
            long completedWorkItemCount = ThreadPool.CompletedWorkItemCount;
#endif

            return new ThreadingStats(completedWorkItemCount, lockContentionCount, 0);
        }

        public string ToOutputLine() => $"{ResultsLinePrefix} {CompletedWorkItemCount} {LockContentionCount} {TotalOperations}";

        public static ThreadingStats Parse(string line)
        {
            if (!line.StartsWith(ResultsLinePrefix))
                throw new NotSupportedException($"Line must start with {ResultsLinePrefix}");

            var measurementSplit = line.Remove(0, ResultsLinePrefix.Length).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (!long.TryParse(measurementSplit[0], out long completedWorkItemCount)
                || !long.TryParse(measurementSplit[1], out long lockContentionCount)
                || !long.TryParse(measurementSplit[2], out long totalOperationsCount))
            {
                throw new NotSupportedException("Invalid string");
            }

            return new ThreadingStats(completedWorkItemCount, lockContentionCount, totalOperationsCount);
        }

        public static ThreadingStats operator +(ThreadingStats left, ThreadingStats right)
            => new ThreadingStats(
                left.CompletedWorkItemCount + right.CompletedWorkItemCount,
                left.LockContentionCount + right.LockContentionCount,
                left.TotalOperations + right.TotalOperations);

        public static ThreadingStats operator -(ThreadingStats left, ThreadingStats right)
            => new ThreadingStats(
                left.CompletedWorkItemCount - right.CompletedWorkItemCount,
                left.LockContentionCount - right.LockContentionCount,
                left.TotalOperations - right.TotalOperations);

        public ThreadingStats WithTotalOperations(long totalOperationsCount) => new ThreadingStats(CompletedWorkItemCount, LockContentionCount, totalOperationsCount);

        public override string ToString() => ToOutputLine();

#if NETSTANDARD2_0
        private static Func<long> CreateGetterDelegate(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName);

            // we create delegate to avoid boxing, IMPORTANT!
            return property != null
                       ? (Func<long>)property.GetGetMethod()!.CreateDelegate(typeof(Func<long>))
                       : () => 0;
        }
#endif
        public bool Equals(ThreadingStats other) => CompletedWorkItemCount == other.CompletedWorkItemCount && LockContentionCount == other.LockContentionCount && TotalOperations == other.TotalOperations;

        public override bool Equals(object? obj) => obj is ThreadingStats other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(CompletedWorkItemCount, LockContentionCount, TotalOperations);
    }
}