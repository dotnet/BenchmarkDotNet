using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace BenchmarkDotNet.Analysers
{
    public class EnvironmentAnalyser : AnalyserBase
    {
        public override string Id => "Environment";
        public static readonly IAnalyser Default = new EnvironmentAnalyser();

        private EnvironmentAnalyser()
        {
        }

        protected override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            if (report.BenchmarkCase.Descriptor.Type.GetTypeInfo().Assembly.IsJitOptimizationDisabled().IsTrue())
                yield return CreateWarning("Benchmark was built without optimization enabled (most probably a DEBUG configuration). Please, build it in RELEASE.", report);
        }

        protected override IEnumerable<Conclusion> AnalyseSummary(Summary summary)
        {
            if (summary.HostEnvironmentInfo.HasAttachedDebugger)
                yield return CreateWarning("Benchmark was executed with attached debugger");

            bool unexpectedExit = summary.Reports.SelectMany(x => x.ExecuteResults).Any(x => x.ExitCode != 0);
            if (unexpectedExit)
            {
                var avProducts = summary.HostEnvironmentInfo.AntivirusProducts.Value;
                if (avProducts.Any())
                    yield return CreateWarning(CreateWarningAboutAntivirus(avProducts));
            }

            var vmHypervisor = summary.HostEnvironmentInfo.VirtualMachineHypervisor.Value;
            if (vmHypervisor != null)
            {
                yield return CreateWarning($"Benchmark was executed on the virtual machine with {vmHypervisor.Name} hypervisor. " +
                                           "Virtualization can affect the measurement result.");
            }
        }

        private static string CreateWarningAboutAntivirus(ICollection<Antivirus> avProducts)
        {
            var sb = new StringBuilder("Detected error exit code from one of the benchmarks. " +
                                       "It might be caused by following antivirus software:");
            sb.AppendLine();

            foreach (var av in avProducts)
                sb.AppendLine($"        - {av}");

            sb.AppendLine($"Use {nameof(InProcessEmitToolchain)} or {nameof(InProcessNoEmitToolchain)} to avoid new process creation.");

            return sb.ToString();
        }
    }
}