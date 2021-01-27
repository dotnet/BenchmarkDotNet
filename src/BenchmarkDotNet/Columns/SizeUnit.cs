using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Columns
{
    [SuppressMessage("ReSharper", "InconsistentNaming")] // We want to use "KB", "MB", "GB", "TB"
    public class SizeUnit : IEquatable<SizeUnit>
    {
        [PublicAPI] public string Name { get; }
        [PublicAPI] public string Description { get; }
        [PublicAPI] public long ByteAmount { get; }

        public SizeUnit(string name, string description, long byteAmount)
        {
            Name = name;
            Description = description;
            ByteAmount = byteAmount;
        }

        private const long BytesInKiloByte = 1024L; // this value MUST NOT be changed

        public SizeValue ToValue(long value = 1) => new SizeValue(value, this);

        [PublicAPI] public static readonly SizeUnit B = new SizeUnit("B", "Byte", 1L);
        [PublicAPI] public static readonly SizeUnit KB = new SizeUnit("KB", "Kilobyte", BytesInKiloByte);
        [PublicAPI] public static readonly SizeUnit MB = new SizeUnit("MB", "Megabyte", BytesInKiloByte * BytesInKiloByte);
        [PublicAPI] public static readonly SizeUnit GB = new SizeUnit("GB", "Gigabyte", BytesInKiloByte * BytesInKiloByte * BytesInKiloByte);
        [PublicAPI] public static readonly SizeUnit TB = new SizeUnit("TB", "Terabyte", BytesInKiloByte * BytesInKiloByte * BytesInKiloByte * BytesInKiloByte);
        [PublicAPI] public static readonly SizeUnit[] All = { B, KB, MB, GB, TB };

        public static SizeUnit GetBestSizeUnit(params long[] values)
        {
            if (!values.Any())
                return B;
            // Use the largest unit to display the smallest recorded measurement without loss of precision.
            long minValue = values.Min();
            foreach (var sizeUnit in All)
            {
                if (minValue < sizeUnit.ByteAmount * BytesInKiloByte)
                    return sizeUnit;
            }
            return All.Last();
        }

        public static double Convert(long value, SizeUnit from, SizeUnit to) => value * (double)from.ByteAmount / (to ?? GetBestSizeUnit(value)).ByteAmount;

        public bool Equals(SizeUnit other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(Name, other.Name) && string.Equals(Description, other.Description) && ByteAmount == other.ByteAmount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((SizeUnit) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ByteAmount.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(SizeUnit left, SizeUnit right) => Equals(left, right);

        public static bool operator !=(SizeUnit left, SizeUnit right) => !Equals(left, right);
    }
}
