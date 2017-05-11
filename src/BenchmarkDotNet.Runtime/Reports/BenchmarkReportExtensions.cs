﻿using System.Linq;
using BenchmarkDotNet.Environments;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Reports
{
    public static class BenchmarkReportExtensions
    {
        private const string DisplayedRuntimeInfoPrefix = "// " + BenchmarkEnvironmentInfo.RuntimeInfoPrefix;
        private const string DisplayedGcInfoPrefix = "// " + BenchmarkEnvironmentInfo.GcInfoPrefix;

        [CanBeNull]
        public static string GetRuntimeInfo(this BenchmarkReport report) => report.GetInfoFromOutput(DisplayedRuntimeInfoPrefix);

        [CanBeNull]
        public static string GetGcInfo(this BenchmarkReport report) => report.GetInfoFromOutput(DisplayedGcInfoPrefix);

        [CanBeNull]
        private static string GetInfoFromOutput(this BenchmarkReport report, string prefix)
        {
            return (
                from executeResults in report.ExecuteResults
                from extraOutputLine in executeResults.ExtraOutput.Where(line => line.StartsWith(prefix))
                select extraOutputLine.Substring(prefix.Length)).FirstOrDefault();
        }
    }
}