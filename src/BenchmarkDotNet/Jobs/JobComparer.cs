using System;
using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;

namespace BenchmarkDotNet.Jobs
{
    internal class JobComparer : IComparer<Job>
    {
        public static readonly IComparer<Job> Instance = new JobComparer();

        public int Compare(Job x, Job y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (x == null)
                return -1;

            if (y == null)
                return 1;

            if (x.GetType()!=y.GetType())
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

                int compare = string.CompareOrdinal(
                    presenter.ToPresentation(x, characteristic),
                    presenter.ToPresentation(y, characteristic));
                if (compare != 0)
                    return compare;
            }

            return 0;
        }
    }
}