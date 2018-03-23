using System;
using System.Collections.Generic;
using BenchmarkDotNet.Configs;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Running
{
    public partial class BenchmarkPartitioner
    {
        public static BuildPartition[] CreateForBuild(BenchmarkRunInfo[] supportedBenchmarks, IResolver resolver)
            => supportedBenchmarks
                .SelectMany(info => info.Benchmarks.Select(benchmark => (benchmark, info.Config)))
                .GroupBy(tuple => tuple.benchmark, BenchmarkRuntimePropertiesComparer.Instance)
                .Select(group => new BuildPartition(group.Select((item, index) => new BenchmarkBuildInfo(item.benchmark, item.Config, index)).ToArray(), resolver))
                .ToArray();

        internal class BenchmarkRuntimePropertiesComparer : IEqualityComparer<Benchmark>
        {
            internal static readonly IEqualityComparer<Benchmark> Instance = new BenchmarkRuntimePropertiesComparer();

            public bool Equals(Benchmark x, Benchmark y)
            {
                Job jobX = x.Job;
                Job jobY = y.Job;

                if (AreDifferent(jobX.GetToolchain(), jobY.GetToolchain())) // Mono vs .NET vs Core vs InProcess
                    return false;
                if (jobX.Env.Jit != jobY.Env.Jit) // Jit is set per exe in .config file
                    return false;
                if (jobX.Env.Platform != jobY.Env.Platform) // platform is set in .csproj
                    return false;
                if (AreDifferent(jobX.Infrastructure.BuildConfiguration, jobY.Infrastructure.BuildConfiguration)) // Debug vs Release
                    return false;
                if (AreDifferent(jobX.Infrastructure.Arguments, jobY.Infrastructure.Arguments)) // arguments can be anything (Mono runtime settings or MsBuild parameters)
                    return false;
                if (!jobX.Env.Gc.Equals(jobY.Env.Gc)) // GC settings are per .config/.csproj
                    return false;

                if (x.Target.Type.Assembly.Location != y.Target.Type.Assembly.Location) // some toolchains produce the exe in the same folder as .dll (to get some scenarios like native dependencies work)
                    return false;

                return true;
            }

            private bool AreDifferent(object x, object y)
            {
                if (x == null && y == null)
                    return false;
                if (x == null || y == null)
                    return true;

                return !x.Equals(y);
            }

            private bool AreDifferent(IReadOnlyList<Argument> x, IReadOnlyList<Argument> y)
            {
                if (x == null && y == null)
                    return false;
                if (x == null || y == null)
                    return true;
                if (x.Count != y.Count)
                    return true;

                for (int i = 0; i < x.Count; i++)
                    if (!x[i].Equals(y[i]))
                        return true;

                return false;
            }

            public int GetHashCode(Benchmark obj) => obj.GetHashCode();
        }
    }
}