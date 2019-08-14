using System;
using System.Threading;

namespace BenchmarkDotNet.Engines
{
    public struct ThreadingStats
    {
        internal const string ResultsLinePrefix = "Threading: ";

        private static readonly Func<long> GetCompletedWorkItemCountDelegate = CreateGetCompletedWorkItemCountDelegate();
        private static readonly Func<long> GetLockContentionCountDelegate = CreateGetLockContentionCountDelegate();

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

        public static ThreadingStats Read()
        {
            long completedWorkItemCount = GetCompletedWorkItemCountDelegate != null ? GetCompletedWorkItemCountDelegate() : default;
            long lockContentionCount = GetLockContentionCountDelegate != null ? GetLockContentionCountDelegate() : default;

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

        public ThreadingStats WithTotalOperations(long totalOperationsCount) => this + new ThreadingStats(0, 0, totalOperationsCount);

        public override string ToString() => ToOutputLine();

        // BDN targets .NET Standard 2.0, these methods are not part of it
        private static Func<long> CreateGetCompletedWorkItemCountDelegate() => CreateGetterDelegate(typeof(ThreadPool), "CompletedWorkItemCount");

        private static Func<long> CreateGetLockContentionCountDelegate() => CreateGetterDelegate(typeof(Monitor), "LockContentionCount");

        private static Func<long> CreateGetterDelegate(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName);

            // we create delegate to avoid boxing, IMPORTANT!
            return property != null ? (Func<long>)property.GetGetMethod().CreateDelegate(typeof(Func<long>)) : null;
        }
    }
}