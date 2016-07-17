using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public class EnvironmentAnalyser : IAnalyser
    {
        public static readonly IAnalyser Default = new EnvironmentAnalyser();

        private EnvironmentAnalyser()
        {
        }

        public IEnumerable<IWarning> Analyse(Summary summary)
        {
            if (summary.HostEnvironmentInfo.HasAttachedDebugger)
                yield return new Warning("AttachedDebugger", "Benchmark was executed with attached debugger.", null);

            foreach (var benchmark in summary.Benchmarks
                .Where(benchmark => benchmark.Target.Type.GetTypeInfo().Assembly.IsDebug().IsTrue()))
            {
                yield return
                    new Warning(
                        "DebugConfiguration",
                        "Benchmark was built in DEBUG configuration. Please, build it in RELEASE.",
                        summary.Reports.FirstOrDefault(report => ReferenceEquals(report.Benchmark, benchmark)));
            }
        }
    }
}