using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Jobs
{
    internal class JobComparer : IComparer<IJob>
    {
        public static readonly IComparer<IJob> Instance = new JobComparer();

        public int Compare(IJob x, IJob y)
        {
            var xp = x.AllProperties;
            var yp = y.AllProperties;
            if (xp.Length != yp.Length)
                throw new InvalidOperationException("xJob.Length != yJob.Length");
            for (int i = 0; i < xp.Length; i++)
            {
                if (xp[i].Name != yp[i].Name)
                    throw new InvalidOperationException($"xJob[{i}].Name != yJob[{i}].Name");
                var jobCompare = string.CompareOrdinal(xp[i].Value, yp[i].Value);
                if (jobCompare != 0)
                    return jobCompare;
            }
            return string.CompareOrdinal(x.GetFullInfo(), y.GetFullInfo());
        }
    }
}