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

        [NotNull]
        public string Message { get; }

        [CanBeNull]
        public BenchmarkReport Report { get; }

        private Conclusion([NotNull] string analyserId, ConclusionKind kind, [NotNull] string message, [CanBeNull] BenchmarkReport report)
        {
            AnalyserId = analyserId;
            Kind = kind;
            Message = message;
            Report = report;
        }

        public static Conclusion CreateHint(string analyserId, string message, [CanBeNull] BenchmarkReport report = null) 
            => new Conclusion(analyserId, ConclusionKind.Hint, message, report);

        public static Conclusion CreateWarning(string analyserId, string message, [CanBeNull] BenchmarkReport report = null) 
            => new Conclusion(analyserId, ConclusionKind.Warning, message, report);

        public static Conclusion CreateError(string analyserId, string message, [CanBeNull] BenchmarkReport report = null) 
            => new Conclusion(analyserId, ConclusionKind.Error, message, report);

        public bool Equals(Conclusion other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
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
                hashCode = (hashCode * 397) ^ Message.GetHashCode();
                return hashCode;
            }
        }
    }
}