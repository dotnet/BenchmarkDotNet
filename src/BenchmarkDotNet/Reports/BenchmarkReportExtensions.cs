using System.Linq;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Reports
{
    public static class BenchmarkReportExtensions
    {
        private const string DisplayedRuntimeInfoPrefix = "// " + BenchmarkEnvironmentInfo.RuntimeInfoPrefix;
        private const string DisplayedGcInfoPrefix = "// " + BenchmarkEnvironmentInfo.GcInfoPrefix;
        private const string DisplayedHardwareIntrinsicsPrefix = "// " + BenchmarkEnvironmentInfo.HardwareIntrinsicsPrefix;

        public static string? GetRuntimeInfo(this BenchmarkReport report) => report.GetInfoFromOutput(DisplayedRuntimeInfoPrefix);

        public static string? GetGcInfo(this BenchmarkReport report) => report.GetInfoFromOutput(DisplayedGcInfoPrefix);

        public static string? GetHardwareIntrinsicsInfo(this BenchmarkReport report) => report.GetInfoFromOutput(DisplayedHardwareIntrinsicsPrefix);

        private static string? GetInfoFromOutput(this BenchmarkReport report, string prefix)
        {
            return (
                from executeResults in report.ExecuteResults
                from extraOutputLine in executeResults.PrefixedLines.Where(line => line.StartsWith(prefix))
                select extraOutputLine.Substring(prefix.Length)).FirstOrDefault();
        }
    }
}