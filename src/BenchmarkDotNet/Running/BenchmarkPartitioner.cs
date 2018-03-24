using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkPartitioner
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

            private static readonly Runtime Current = RuntimeInformation.GetCurrentRuntime();

            public bool Equals(Benchmark x, Benchmark y)
            {
                Job jobX = x.Job;
                Job jobY = y.Job;

                if (AreDifferent(GetRuntime(jobX), GetRuntime(jobY))) // Mono vs .NET vs Core
                    return false;
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

                if (x.Target.AdditionalLogic != y.Target.AdditionalLogic) // it can be anything
                    return false;

                if (x.Target.Method.GetCustomAttributes(false).OfType<STAThreadAttribute>().Count() !=
                    y.Target.Method.GetCustomAttributes(false).OfType<STAThreadAttribute>().Count()) // STA vs STA
                    return false;

                return true;
            }

            public int GetHashCode(Benchmark obj)
            {
                var job = obj.Job;

                int hashCode = job.GetToolchain().GetHashCode();

                hashCode ^=  GetRuntime(job).GetType().MetadataToken;

                hashCode ^= obj.Target.Type.Assembly.Location.GetHashCode();
                hashCode ^= (int)job.Env.Jit;
                hashCode ^= (int)job.Env.Platform;
                hashCode ^= job.Env.Gc.GetHashCode();

                if (job.Infrastructure.BuildConfiguration != null)
                    hashCode ^= job.Infrastructure.BuildConfiguration.GetHashCode();
                if (job.Infrastructure.Arguments != null && job.Infrastructure.Arguments.Any())
                    hashCode ^= job.Infrastructure.Arguments.GetHashCode();
                if (!string.IsNullOrEmpty(obj.Target.AdditionalLogic))
                    hashCode ^= obj.Target.AdditionalLogic.GetHashCode();

                hashCode ^= obj.Target.Method.GetCustomAttributes(false).OfType<STAThreadAttribute>().Any().GetHashCode();

                return hashCode;
            }

            private Runtime GetRuntime(Job job)
                => job.Env.HasValue(EnvMode.RuntimeCharacteristic)
                    ? job.Env.Runtime
                    : Current;

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
        }
    }
}