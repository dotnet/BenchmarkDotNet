using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Extensions
{
    public static class ReportExtensions
    {
        public static BenchmarkReport GetReportFor<T>(this Summary summary, Expression<Action<T>> actionExp)
        {
            if (actionExp.Body == null)
                throw new ArgumentException("Extend a an Expression with a valid Body", nameof(actionExp));

            if (!(actionExp.Body is MethodCallExpression methodExp))
                throw new ArgumentException("Extend a MethodCallExpression, but got a " + actionExp.Body.GetType().Name, nameof(actionExp));

            return summary.Reports.First(r => r.BenchmarkCase.Descriptor.WorkloadMethod == methodExp.Method);
        }

        public static IList<Measurement> GetRunsFor<T>(this Summary summary, Expression<Action<T>> actionExp)
        {
            return summary.GetReportFor(actionExp).GetResultRuns().ToList();
        }

        public static Statistics GetStatistics(this IReadOnlyCollection<Measurement> runs)
        {
            if (runs.IsEmpty())
                throw new InvalidOperationException("List of measurements contains no elements");
            return new Statistics(runs.Select(r => r.GetAverageTime().Nanoseconds));
        }

        public static Statistics GetStatistics(this IEnumerable<Measurement> runs) =>
            GetStatistics(runs.ToList());

        public static bool HasError(this IEnumerable<Summary> summaries)
        {
            if (summaries.Count() == 0)
            {
                // When following argument specified. BenchmarkDotNet show information only.
                var knownArguments = new HashSet<string>(["--help", "--list", "--info", "--version"]);
                return !Environment.GetCommandLineArgs().Any(knownArguments.Contains);
            }

            if (summaries.Any(x => x.HasCriticalValidationErrors))
                return true;

            return summaries.Any(x => x.Reports.Any(r => !r.Success));
        }
    }
}
