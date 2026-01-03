using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers
{
    /// <summary>
    /// Represents a custom hardware performance counter that can be specified by its ETW profile source name.
    /// Use this when the predefined <see cref="HardwareCounter"/> enum values don't match the counters 
    /// available on your machine (e.g., AMD-specific counters like DcacheMisses, IcacheMisses).
    /// Run <c>TraceEventProfileSources.GetInfo().Keys</c> to discover available counters on your system.
    /// </summary>
    public class CustomCounter
    {
        /// <summary>
        /// Default sampling interval for custom counters.
        /// </summary>
        public const int DefaultInterval = 1_000_003;

        /// <summary>
        /// The exact name of the ETW profile source as returned by TraceEventProfileSources.GetInfo().
        /// </summary>
        [PublicAPI]
        public string ProfileSourceName { get; }

        /// <summary>
        /// A short name used for display in reports and columns.
        /// </summary>
        [PublicAPI]
        public string ShortName { get; }

        /// <summary>
        /// The sampling interval for this counter.
        /// </summary>
        [PublicAPI]
        public int Interval { get; }

        /// <summary>
        /// Indicates whether higher values are better for this counter.
        /// Default is false (lower is better, e.g., cache misses).
        /// </summary>
        [PublicAPI]
        public bool HigherIsBetter { get; }

        /// <summary>
        /// Creates a new custom hardware counter.
        /// </summary>
        /// <param name="profileSourceName">The exact name of the ETW profile source (e.g., "DcacheMisses", "IcacheMisses").</param>
        /// <param name="shortName">Optional short name for display. If null, uses the profile source name.</param>
        /// <param name="interval">Sampling interval. If not specified, uses DefaultInterval (1,000,003).</param>
        /// <param name="higherIsBetter">Whether higher values are better for this counter.</param>
        public CustomCounter(string profileSourceName, string? shortName = null, int interval = DefaultInterval, bool higherIsBetter = false)
        {
            if (profileSourceName == null)
                throw new ArgumentNullException(nameof(profileSourceName));
            if (string.IsNullOrWhiteSpace(profileSourceName))
                throw new ArgumentException("Profile source name cannot be empty or whitespace.", nameof(profileSourceName));

            ProfileSourceName = profileSourceName;
            ShortName = shortName ?? profileSourceName;
            Interval = interval;
            HigherIsBetter = higherIsBetter;
        }

        public override string ToString() => ProfileSourceName;

        public override bool Equals(object? obj)
            => obj is CustomCounter other && ProfileSourceName == other.ProfileSourceName;

        public override int GetHashCode() => ProfileSourceName.GetHashCode();
    }
}
