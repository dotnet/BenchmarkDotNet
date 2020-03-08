using System;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Analysers
{
    // TODO: Find a better name
    public sealed class Conclusion : IEquatable<Conclusion>
    {
        [NotNull]
        public string AnalyserId { get; }

        public ConclusionKind Kind { get; }

        public bool Mergeable { get; }

        [NotNull]
        public string Message { get; }

        [CanBeNull]
        public BenchmarkReport Report { get; }

        private Conclusion([NotNull] string analyserId,
                           ConclusionKind kind,
                           [NotNull] string message,
                           [CanBeNull] BenchmarkReport report,
                           bool mergeable)
        {
            AnalyserId = analyserId;
            Kind = kind;
            Message = message;
            Report = report;
            Mergeable = mergeable;
        }

        public static Conclusion CreateHint(string analyserId, string message, [CanBeNull] BenchmarkReport report = null, bool mergeable = true)
            => new Conclusion(analyserId, ConclusionKind.Hint, message, report, mergeable);

        public static Conclusion CreateWarning(string analyserId, string message, [CanBeNull] BenchmarkReport report = null, bool mergeable = true)
            => new Conclusion(analyserId, ConclusionKind.Warning, message, report, mergeable);

        public static Conclusion CreateError(string analyserId, string message, [CanBeNull] BenchmarkReport report = null, bool mergeable = true)
            => new Conclusion(analyserId, ConclusionKind.Error, message, report, mergeable);

        public bool Equals(Conclusion other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (!Mergeable && !other.Mergeable)
                return string.Equals(AnalyserId, other.AnalyserId) && Kind == other.Kind && string.Equals(Report?.ToString(), other.Report?.ToString());
            return string.Equals(AnalyserId, other.AnalyserId) && Kind == other.Kind && string.Equals(Message, other.Message);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj is Conclusion conclusion && Equals(conclusion);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = AnalyserId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Kind;
                hashCode = Mergeable
                           ? (hashCode * 397) ^ Message.GetHashCode()
                           : (hashCode * 397) ^ Report?.ToString().GetHashCode() ?? string.Empty.GetHashCode();
                return hashCode;
            }
        }
    }
}