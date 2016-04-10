using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analyzers
{
    public class JitOptimizationsAnalyser : IAnalyser
    {
        public static readonly IAnalyser Instance = new JitOptimizationsAnalyser();

        private JitOptimizationsAnalyser()
        {
        }

        public IEnumerable<IWarning> Analyze(Summary summary)
        {
#if CORE
            yield break; // todo: implement when it becomes possible
#else
            foreach (var report in summary.Reports)
            {
                foreach (var referencedAssemblyName in report.Benchmark.Target.Type.Assembly().GetReferencedAssemblies())
                {
                    var referencedAssembly = Assembly.Load(referencedAssemblyName);

                    if (IsJITOptimizationDisabled(referencedAssembly))
                    {
                        yield return new Warning(
                            "NonOptimizedDll", 
                            $"Benchmark {report.Benchmark.ShortInfo} is defined in assembly that references non-optimized {referencedAssemblyName.Name}",
                            report);
                    }
                }

                if (IsJITOptimizationDisabled(report.Benchmark.Target.Type.Assembly()))
                {
                    yield return new Warning(
                        "NonOptimizedDll",
                        $"Benchmark {report.Benchmark.ShortInfo} is defined in non-optimized dll",
                        report);
                }
            }
#endif
        }

#if !CORE
        private bool IsJITOptimizationDisabled(Assembly assembly)
        {
            return assembly
                .GetCustomAttributes<DebuggableAttribute>(false)
                .Any(attribute => attribute.IsJITOptimizerDisabled);
        }
#endif
    }
}