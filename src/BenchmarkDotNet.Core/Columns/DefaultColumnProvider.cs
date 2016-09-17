using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using System.Linq;

namespace BenchmarkDotNet.Columns
{
    public static class DefaultColumnProviders
    {
        public static readonly IColumnProvider Target = new TargetColumnProvider();
        public static readonly IColumnProvider Job = new JobColumnProvider();
        public static readonly IColumnProvider Statistics = new StatisticsColumnProvider();
        public static readonly IColumnProvider Params = new ParamsColumnProvider();
        public static readonly IColumnProvider Diagnosers = new DiagnosersColumnProvider();

        public static readonly IColumnProvider[] Instance = { Target, Job, Statistics, Params, Diagnosers };

        private class TargetColumnProvider : IColumnProvider
        {
            public IEnumerable<IColumn> GetColumns(Summary summary)
            {
                if (summary.Benchmarks.Select(b => b.Target.Type.Namespace).Distinct().Count() > 1)
                    yield return TargetMethodColumn.Namespace;
                if (summary.Benchmarks.Select(b => b.Target.Type.Name).Distinct().Count() > 1)
                    yield return TargetMethodColumn.Type;
                yield return TargetMethodColumn.Method;
            }
        }

        private class JobColumnProvider : IColumnProvider
        {
            public IEnumerable<IColumn> GetColumns(Summary summary) => CharacteristicColumn.AllColumns;
        }

        private class StatisticsColumnProvider : IColumnProvider
        {
            public IEnumerable<IColumn> GetColumns(Summary summary)
            {
                yield return StatisticColumn.Mean;
                yield return StatisticColumn.StdErr;
                // TODO: Meadian, P95
                if (summary.Reports != null && summary.Reports.Any(r => r.ResultStatistics != null && r.ResultStatistics.StandardDeviation > 1e-9))
                    yield return StatisticColumn.StdDev;

                yield return BaselineScaledColumn.Scaled;
                yield return BaselineScaledColumn.ScaledStdDev; // TODO: make optional
            }
        }

        private class ParamsColumnProvider : IColumnProvider
        {
            public IEnumerable<IColumn> GetColumns(Summary summary)
            {
                return summary.Benchmarks.
                    SelectMany(b => b.Parameters.Items.Select(item => item.Name)).
                    Distinct().
                    Select(name => new ParamColumn(name));
            }
        }

        private class DiagnosersColumnProvider : IColumnProvider
        {
            public IEnumerable<IColumn> GetColumns(Summary summary) =>
                summary.Config.
                    GetDiagnosers().
                    Select(d => d.GetColumnProvider()).
                    SelectMany(cp => cp.GetColumns(summary));
        }
    }
}