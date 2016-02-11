using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Parameters;

namespace BenchmarkDotNet.Reports
{
    internal class ParameterComparer : IComparer<ParameterInstances>
    {
        public int Compare(ParameterInstances x, ParameterInstances y)
        {
            if (x.Items.Count == 0 || y.Items.Count == 0)
                return x.FullInfo.CompareTo(y.FullInfo);

            // Have a hard-stop, so we don't loop forever (5 Param fields/properties should be enough)!!!
            var skip = 0;
            while (skip < 5)
            {
                var xItem = x.Items.Skip(skip).FirstOrDefault();
                var yItem = y.Items.Skip(skip).FirstOrDefault();
                skip++;

                if (xItem != null && yItem != null)
                {
                    // If x and y match (at the same position), goto the next position
                    if (CompareTo(xItem, yItem) == 0)
                        continue;

                    return CompareTo(xItem, yItem);
                }
                else
                {
                    // If we can't get a value, use the fallback comparison
                    return x.FullInfo.CompareTo(y.FullInfo);
                }
            }

            // Fallback comparison
            return x.FullInfo.CompareTo(y.FullInfo);
        }

        private int CompareTo(ParameterInstance x, ParameterInstance y)
        {
            // This is a best-effort and we only try and compare if the types match!!
            if (x.Value.GetType() != y.Value.GetType())
                return x.Value.ToString().CompareTo(y.Value.ToString());

            if (x.Value is string && y.Value is string)
                return x.Value.ToString().CompareTo(y.Value.ToString());

            // We will only worry about common, basic types, i.e. int, long, double, etc
            // (e.g. you can't write [Params(10.0m, 20.0m, 100.0m, 200.0m)], the compiler won't let you!)
            if (x.Value is int && y.Value is int)
                return ((int)x.Value).CompareTo((int)y.Value);
            if (x.Value is long && y.Value is long)
                return ((long)x.Value).CompareTo((long)y.Value);
            if (x.Value is short && y.Value is short)
                return ((short)x.Value).CompareTo((short)y.Value);

            if (x.Value is float && y.Value is float)
                return ((float)x.Value).CompareTo((float)y.Value);
            if (x.Value is double && y.Value is double)
                return ((double)x.Value).CompareTo((double)y.Value);

            // Fallback, if all else fails
            return x.Value.ToString().CompareTo(y.Value.ToString());
        }
    }
}
