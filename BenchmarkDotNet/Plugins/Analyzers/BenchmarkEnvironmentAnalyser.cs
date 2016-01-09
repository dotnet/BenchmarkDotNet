using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Analyzers
{
    public class BenchmarkEnvironmentAnalyser : IBenchmarkAnalyser
    {
        public static readonly IBenchmarkAnalyser Default = new BenchmarkEnvironmentAnalyser();

        public string Name => "env";
        public string Description => "Environment analyser";

        public IEnumerable<IBenchmarkAnalysisWarning> Analyze(IEnumerable<BenchmarkReport> reports)
        {
            var firstReport = reports.FirstOrDefault();
            if (firstReport != null)
            {
                var hostInfo = firstReport.HostInfo;
                if (hostInfo.HasAttachedDebugger)
                    yield return new BenchmarkAnalysisWarning("AttachedDebugger", "Benchmark was executed with attached debugger.", null);
                if (hostInfo.Configuration.Equals("DEBUG", StringComparison.InvariantCultureIgnoreCase))
                    yield return new BenchmarkAnalysisWarning("DebugConfiguration", "Benchmark was built in DEBUG configuration. Please, build it in RELEASE.", null);
            }
        }
    }
}