using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Environments
{
    public class R2RRuntime : Runtime
    {
        public static readonly R2RRuntime Net80 = new R2RRuntime(RuntimeMoniker.R2R80, "net8.0", "R2R 8.0");
        public static readonly R2RRuntime Net90 = new R2RRuntime(RuntimeMoniker.R2R90, "net9.0", "R2R 9.0");
        public static readonly R2RRuntime Net10_0 = new R2RRuntime(RuntimeMoniker.R2R10_0, "net10.0", "R2R 10.0");
        public static readonly R2RRuntime Net11_0 = new R2RRuntime(RuntimeMoniker.R2R11_0, "net11.0", "R2R 11.0");

        private R2RRuntime(RuntimeMoniker runtimeMoniker, string msBuildMoniker, string displayName)
            : base(runtimeMoniker, msBuildMoniker, displayName)
        {
        }
    }
}
