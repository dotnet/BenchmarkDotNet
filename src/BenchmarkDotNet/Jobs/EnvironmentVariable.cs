using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Jobs
{
    public class EnvironmentVariable : IEquatable<EnvironmentVariable>
    {
        public EnvironmentVariable([NotNull] string key, [NotNull] string value)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value ?? throw new ArgumentNullException(nameof(Value));
        }

        [NotNull]
        public string Key { get; }

        [NotNull]
        public string Value { get; }

        // CharacteristicPresenters call ToString(), this is why we need this override
        public override string ToString() => $"{Key}={Value}";

        public bool Equals(EnvironmentVariable other) => string.Equals(Key, other.Key) && string.Equals(Value, other.Value);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((EnvironmentVariable) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Key.GetHashCode() * 397) ^ Value.GetHashCode();
            }
        }
    }
}