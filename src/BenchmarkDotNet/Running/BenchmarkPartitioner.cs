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
    public static class BenchmarkPartitioner
    {
        public static BuildPartition[] CreateForBuild(BenchmarkRunInfo[] supportedBenchmarks, IResolver resolver)
            => supportedBenchmarks
                .SelectMany(info => info.BenchmarksCases.Select(benchmark => (benchmark, benchmark.Config)))
                .GroupBy(tuple => tuple.benchmark, BenchmarkRuntimePropertiesComparer.Instance)
                .Select(group => new BuildPartition(group.Select((item, index) => new BenchmarkBuildInfo(item.benchmark, item.Config, index)).ToArray(), resolver))
                .ToArray();

        internal class BenchmarkRuntimePropertiesComparer : IEqualityComparer<BenchmarkCase>
        {
            internal static readonly IEqualityComparer<BenchmarkCase> Instance = new BenchmarkRuntimePropertiesComparer();

            private static readonly Runtime Current = RuntimeInformation.GetCurrentRuntime();

            public bool Equals(BenchmarkCase x, BenchmarkCase y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null || y == null)
                    return false;
                var jobX = x.Job;
                var jobY = y.Job;

                if (AreDifferent(GetRuntime(jobX), GetRuntime(jobY))) // Mono vs .NET vs Core
                    return false;
                if (AreDifferent(x.GetToolchain(), y.GetToolchain())) // Mono vs .NET vs Core vs InProcess
                    return false;
                if (jobX.Environment.Jit != jobY.Environment.Jit) // Jit is set per exe in .config file
                    return false;
                if (jobX.Environment.Platform != jobY.Environment.Platform) // platform is set in .csproj
                    return false;
                if (jobX.Environment.LargeAddressAware != jobY.Environment.LargeAddressAware)
                    return false;
                if (AreDifferent(jobX.Infrastructure.BuildConfiguration, jobY.Infrastructure.BuildConfiguration)) // Debug vs Release
                    return false;
                if (AreDifferent(jobX.Infrastructure.Arguments, jobY.Infrastructure.Arguments)) // arguments can be anything (Mono runtime settings or MsBuild parameters)
                    return false;
                if (AreDifferent(jobX.Infrastructure.NuGetReferences, jobY.Infrastructure.NuGetReferences))
                    return false;
                if (!jobX.Environment.Gc.Equals(jobY.Environment.Gc)) // GC settings are per .config/.csproj
                    return false;

                if (x.Descriptor.Type.Assembly.Location != y.Descriptor.Type.Assembly.Location) // some toolchains produce the exe in the same folder as .dll (to get some scenarios like native dependencies work)
                    return false;

                if (x.Descriptor.AdditionalLogic != y.Descriptor.AdditionalLogic) // it can be anything
                    return false;

                if (x.Descriptor.WorkloadMethod.GetCustomAttributes(false).OfType<STAThreadAttribute>().Count() !=
                    y.Descriptor.WorkloadMethod.GetCustomAttributes(false).OfType<STAThreadAttribute>().Count()) // STA vs STA
                    return false;

                return true;
            }

            public int GetHashCode(BenchmarkCase obj)
            {
                var hashCode = new HashCode();
                hashCode.Add(obj.GetToolchain());
                hashCode.Add(obj.GetRuntime());
                hashCode.Add(obj.Descriptor.Type.Assembly.Location);
                hashCode.Add(obj.Descriptor.AdditionalLogic);
                hashCode.Add(obj.Descriptor.WorkloadMethod.GetCustomAttributes(false).OfType<STAThreadAttribute>().Any());
                var job = obj.Job;
                hashCode.Add(job.Environment.Jit);
                hashCode.Add(job.Environment.Platform);
                hashCode.Add(job.Environment.LargeAddressAware);
                hashCode.Add(job.Environment.Gc);
                hashCode.Add(job.Infrastructure.BuildConfiguration);
                foreach (var arg in job.Infrastructure.Arguments ?? Array.Empty<Argument>())
                    hashCode.Add(arg);
                foreach (var reference in job.Infrastructure.NuGetReferences ?? Array.Empty<NuGetReference>())
                    hashCode.Add(reference);
                return hashCode.ToHashCode();
            }

            private static Runtime GetRuntime(Job job)
                => job.Environment.HasValue(EnvironmentMode.RuntimeCharacteristic)
                    ? job.Environment.Runtime
                    : Current;

            private static bool AreDifferent(object x, object y)
            {
                if (x == null && y == null)
                    return false;
                if (x == null || y == null)
                    return true;

                return !x.Equals(y);
            }

            private static bool AreDifferent(IReadOnlyList<Argument> x, IReadOnlyList<Argument> y)
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