using System;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Validators
{
    public class ValidationError : IEquatable<ValidationError>
    {
        public ValidationError(bool isCritical, string message, BenchmarkCase? benchmarkCase = null)
        {
            IsCritical = isCritical;
            Message = message;
            BenchmarkCase = benchmarkCase;
        }

        [PublicAPI] public bool IsCritical { get; }
        [PublicAPI] public string Message { get; }
        [PublicAPI] public BenchmarkCase? BenchmarkCase { get; }

        public override string ToString() => Message;

        public bool Equals(ValidationError? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return IsCritical == other.IsCritical && string.Equals(Message, other.Message) && Equals(BenchmarkCase, other.BenchmarkCase);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((ValidationError)obj);
        }

        public override int GetHashCode() => HashCode.Combine(IsCritical, Message, BenchmarkCase);

        public static bool operator ==(ValidationError left, ValidationError right) => Equals(left, right);

        public static bool operator !=(ValidationError left, ValidationError right) => !Equals(left, right);
    }
}