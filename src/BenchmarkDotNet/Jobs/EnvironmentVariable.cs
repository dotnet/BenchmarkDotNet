using System;

namespace BenchmarkDotNet.Jobs
{
    public class EnvironmentVariable : IEquatable<EnvironmentVariable>
    {
        public EnvironmentVariable(string key, string value)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Key { get; }

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

        public override int GetHashCode() => HashCode.Combine(Key, Value);
    }
}