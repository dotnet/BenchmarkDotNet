using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Jobs
{
    internal class JobComparer : IComparer<Job>
    {
        public static readonly IComparer<Job> Instance = new JobComparer();

        public int Compare(Job x, Job y)
        {
            var xp = x.ToSet().GetValues();
            var yp = y.ToSet().GetValues();
            if (xp.Length != yp.Length)
                throw new InvalidOperationException("xJob.Length != yJob.Length");
            for (int i = 0; i < xp.Length; i++)
            {
                if (xp[i].Id != yp[i].Id)
                    throw new InvalidOperationException($"xJob[{i}].Id != yJob[{i}].Id");
                if (xp[i].IsDefault && yp[i].IsDefault)
                    continue;
                if (xp[i].IsDefault && !yp[i].IsDefault)
                    return 1;
                if (!xp[i].IsDefault && yp[i].IsDefault)
                    return -1;
                var jobCompare = string.CompareOrdinal(xp[i].ObjectValue.ToString(), yp[i].ObjectValue.ToString());
                if (jobCompare != 0)
                    return jobCompare;
            }
            return string.CompareOrdinal(x.DisplayInfo, y.DisplayInfo);
        }
    }
}