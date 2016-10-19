using System;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class CharacteristicColumn : IColumn
    {
        private static readonly CharacteristicPresenter Presenter = CharacteristicPresenter.SummaryPresenter;

        private readonly Characteristic characteristic;

        public CharacteristicColumn(Characteristic characteristic)
        {
            this.characteristic = characteristic;
        }


        public string Id => "Job." + characteristic.Id;
        public string ColumnName => characteristic.Id;
        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => false;
        public ColumnCategory Category => ColumnCategory.Job;
        public int PriorityInCategory => 0;

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
                .Select(c => (IColumn)new CharacteristicColumn(c)).ToArray());

        public static IColumn[] AllColumns => LazyAllColumns.Value;
    }
}