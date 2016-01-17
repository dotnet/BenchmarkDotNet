using BenchmarkDotNet.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Statistic;

namespace BenchmarkDotNet.Extensions
{
    public static class ReportExtensions
    {
        public static BenchmarkReport GetReportFor<T>(this IEnumerable<BenchmarkReport> reports, Expression<Action<T>> actionExp)
        {
            if (actionExp.Body == null)
                throw new ArgumentException("Extend a an Expression with a valid Body", nameof(actionExp));

            var methodExp = actionExp.Body as MethodCallExpression;
            if (methodExp == null)
                throw new ArgumentException("Extend a MethodCallExpression, but got a " + actionExp.Body.GetType().Name, nameof(actionExp));

            return reports.First(r => r.Benchmark.Target.Method == methodExp.Method);
        }

        public static IList<BenchmarkRunReport> GetRunsFor<T>(this IEnumerable<BenchmarkReport> reports, Expression<Action<T>> actionExp)
        {
            return reports.GetReportFor<T>(actionExp).GetTargetRuns().ToList();
        }

        public static StatSummary GetStats(this IList<BenchmarkRunReport> runs) =>
            runs.Any()
            ? new StatSummary(runs.Select(r => r.GetAverageNanoseconds()))
            : null;

        public static StatSummary GetStats(this IEnumerable<BenchmarkRunReport> runs) =>
            GetStats(runs.ToList());
    }
}
