using System;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

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
            if (summary.IsMultipleRuntimes)
            {
                if (nameof(Toolchains.Toolchain).Equals(ColumnName))
                {
                    return false;
                }
                if (nameof(Job).Equals(ColumnName))
                {
                    return summary.BenchmarksCases.Any(x => x.Job.HasValue(CharacteristicObject.IdCharacteristic));
                }
            }

            return true;
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