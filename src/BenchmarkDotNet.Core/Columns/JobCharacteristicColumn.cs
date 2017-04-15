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

        public JobCharacteristicColumn(Characteristic characteristic)
        {
            this.characteristic = characteristic;
            Id = "Job." + characteristic.Id;
            ColumnName = characteristic.Id;

            // The 'Id' characteristic is a special case:
            // here we just print 'Job'
            if (characteristic.Id == "Id")
                ColumnName = "Job";
        }

        public string Id { get; }
        public string ColumnName { get; }
        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => false;
        public ColumnCategory Category => ColumnCategory.Job;
        public int PriorityInCategory => 0;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style) => GetValue(summary, benchmark);

        public bool IsDefault(Summary summary, Benchmark benchmark) => !benchmark.Job.HasValue(characteristic);

        public string GetValue(Summary summary, Benchmark benchmark)
        {
            if (!benchmark.Job.HasValue(characteristic) && EnvResolver.Instance.CanResolve(characteristic))
                return benchmark.Job.ResolveValue(characteristic, EnvResolver.Instance).ToString();
            return Presenter.ToPresentation(benchmark.Job, characteristic);
        }

        private static readonly Lazy<IColumn[]> LazyAllColumns =
            new Lazy<IColumn[]>(() =>
                CharacteristicHelper.GetAllPresentableCharacteristics(typeof(Job), true)
                .Select(c => (IColumn)new JobCharacteristicColumn(c)).ToArray());

        public static IColumn[] AllColumns => LazyAllColumns.Value;

        public string Legend => "";
    }
}