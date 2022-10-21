using System;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Columns
{
    public class JobCharacteristicColumn : IColumn
    {
        private static readonly CharacteristicPresenter Presenter = CharacteristicPresenter.SummaryPresenter;

        private readonly Characteristic characteristic;

        private JobCharacteristicColumn(Characteristic characteristic)
        {
            this.characteristic = characteristic;
            Id = "Job." + characteristic.Id;
            ColumnName = characteristic.Id;

            // The 'Id' characteristic is a special case:
            // here we just print 'Job'
            if (characteristic.Id == "Id")
                ColumnName = Column.Job;
        }

        public string Id { get; }
        public string ColumnName { get; }
        public bool AlwaysShow => false;
        public ColumnCategory Category => ColumnCategory.Job;
        public int PriorityInCategory => 0;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => !benchmarkCase.Job.HasValue(characteristic);

        public bool IsAvailable(Summary summary)
        {
            switch (ColumnName)
            {
                case Column.Job:
                    return false;
                case Column.Toolchain:
                    var groups = summary.BenchmarksCases.GroupBy(b => b.GetRuntime(), b => b.GetToolchain().Name).ToArray();

                    bool isOneRuntime = groups.Length <= 1;
                    if (isOneRuntime)
                        return true;

                    return groups.Any(toolchainNames => toolchainNames.Distinct().Count() > 1);
                default:
                    return true;
            }
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            if (!benchmarkCase.Job.HasValue(characteristic) && EnvironmentResolver.Instance.CanResolve(characteristic))
                return Presenter.ToPresentation(benchmarkCase.Job.ResolveValue(characteristic, EnvironmentResolver.Instance), characteristic);

            return Presenter.ToPresentation(benchmarkCase.Job, characteristic);
        }

        private static readonly Lazy<IColumn[]> LazyAllColumns =
            new Lazy<IColumn[]>(() =>
                CharacteristicHelper.GetAllPresentableCharacteristics(typeof(Job), true)
                .Select(c => (IColumn)new JobCharacteristicColumn(c)).ToArray());

        public static IColumn[] AllColumns => LazyAllColumns.Value;

        public string Legend => "";
    }
}