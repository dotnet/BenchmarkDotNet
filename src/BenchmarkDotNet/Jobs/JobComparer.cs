using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Order;
using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Jobs
{
    internal class JobComparer : IComparer<Job>, IEqualityComparer<Job>
    {
        private readonly IComparer<string> Comparer;

        public static readonly JobComparer Instance = new JobComparer(JobOrderPolicy.Default);
        public static readonly JobComparer Numeric = new JobComparer(JobOrderPolicy.Numeric);

        public JobComparer(JobOrderPolicy jobOrderPolicy = JobOrderPolicy.Default)
        {
            Comparer = jobOrderPolicy == JobOrderPolicy.Default
                ? StringComparer.Ordinal
                : new NumericStringComparer();  // TODO: Use `StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering)` for .NET10 or greater.
        }

        public int Compare(Job x, Job y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (x == null)
                return -1;

            if (y == null)
                return 1;

            if (x.GetType() != y.GetType())
                throw new InvalidOperationException($"The type of xJob ({x.GetType()}) != type of yJob ({y.GetType()})");

            var presenter = CharacteristicPresenter.DefaultPresenter;

            foreach (var characteristic in x.GetAllCharacteristics())
            {
                if (!x.HasValue(characteristic))
                {
                    if (y.HasValue(characteristic))
                        return -1;
                    continue;
                }
                if (!y.HasValue(characteristic))
                {
                    if (x.HasValue(characteristic))
                        return 1;
                    continue;
                }

                int compare = Comparer.Compare(
                    presenter.ToPresentation(x, characteristic),
                    presenter.ToPresentation(y, characteristic));
                if (compare != 0)
                    return compare;
            }

            return 0;
        }

        public bool Equals(Job x, Job y) => Compare(x, y) == 0;

        public int GetHashCode(Job obj) => obj.Id.GetHashCode();

        internal class NumericStringComparer : IComparer<string>
        {
            public int Compare(string? x, string? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                ReadOnlySpan<char> spanX = x.AsSpan();
                ReadOnlySpan<char> spanY = y.AsSpan();

                int i = 0, j = 0;

                while (i < spanX.Length && j < spanY.Length)
                {
                    char cx = spanX[i];
                    char cy = spanY[j];

                    if (!char.IsDigit(cx) || !char.IsDigit(cy))
                    {
                        int cmp = cx.CompareTo(cy);
                        if (cmp != 0)
                            return cmp;

                        i++;
                        j++;
                        continue;
                    }

                    int ixStart = i;
                    int iyStart = j;

                    // Skip leading zeros
                    while (ixStart < spanX.Length && spanX[ixStart] == '0') ixStart++;
                    while (iyStart < spanY.Length && spanY[iyStart] == '0') iyStart++;

                    int ix = ixStart;
                    int iy = iyStart;

                    // Skip digits
                    while (ix < spanX.Length && char.IsDigit(spanX[ix])) ix++;
                    while (iy < spanY.Length && char.IsDigit(spanY[iy])) iy++;

                    int lenX = ix - ixStart;
                    int lenY = iy - iyStart;

                    // Compare by digits length
                    if (lenX != lenY)
                        return lenX.CompareTo(lenY);

                    // Compare digits
                    for (int k = 0; k < lenX; k++)
                    {
                        int cmp = spanX[ixStart + k].CompareTo(spanY[iyStart + k]);
                        if (cmp != 0)
                            return cmp;
                    }

                    // Compare by leading zeros
                    int leadingZerosX = ixStart - i;
                    int leadingZerosY = iyStart - j;
                    if (leadingZerosX != leadingZerosY)
                        return 0; // Leading zero differences are ignored (`CompareOptions.NumericOrdering` behavior of .NET)

                    // Move to the next character after the digits
                    i = ix;
                    j = iy;
                }

                // Compare remaining chars
                return (spanX.Length - i).CompareTo(spanY.Length - j);
            }
        }
    }
}