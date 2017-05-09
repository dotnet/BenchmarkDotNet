using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Columns
{
    public class SizeUnit
    {
        public string Name { get; }
        public string Description { get; }
        public long ByteAmount { get; }

        public SizeUnit(string name, string description, long byteAmount)
        {
            Name = name;
            Description = description;
            ByteAmount = byteAmount;
        }

        public const long BytesInKiloByte = 1024L; // this value MUST NOT be changed
        public static readonly SizeUnit B = new SizeUnit("B", "Byte", 1L);
        public static readonly SizeUnit KB = new SizeUnit("KB", "Kilobyte", BytesInKiloByte);
        public static readonly SizeUnit MB = new SizeUnit("MB", "Megabyte", BytesInKiloByte * BytesInKiloByte);
        public static readonly SizeUnit GB = new SizeUnit("GB", "Gigabyte", BytesInKiloByte * BytesInKiloByte * BytesInKiloByte);
        public static readonly SizeUnit TB = new SizeUnit("TB", "Terabyte", BytesInKiloByte * BytesInKiloByte * BytesInKiloByte * BytesInKiloByte);
        public static readonly SizeUnit[] All = { B, KB, MB, GB, TB };

        public static SizeUnit GetBestSizeUnit(params long[] values)
        {
            if (!values.Any())
                return SizeUnit.B;
            // Use the largest unit to display the smallest recorded measurement without loss of precision.
            var minValue = values.Min();
            foreach (var sizeUnit in All)
            {
                if (minValue < sizeUnit.ByteAmount * BytesInKiloByte)
                    return sizeUnit;
            }
            return All.Last();
        }

        public static double Convert(long value, SizeUnit from, SizeUnit to) => value * (double)@from.ByteAmount / (to ?? GetBestSizeUnit(value)).ByteAmount;
    }
}
