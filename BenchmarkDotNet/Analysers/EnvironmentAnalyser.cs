using System.Collections.Generic;
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
            var hostInfo = summary.HostEnvironmentInfo;
            if (hostInfo.HasAttachedDebugger)
                yield return new Warning("AttachedDebugger", "Benchmark was executed with attached debugger.", null);
            if (hostInfo.Configuration.EqualsWithIgnoreCase("DEBUG"))
                yield return new Warning("DebugConfiguration", "Benchmark was built in DEBUG configuration. Please, build it in RELEASE.", null);
        }
    }
}