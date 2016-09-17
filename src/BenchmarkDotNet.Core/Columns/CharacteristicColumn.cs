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

        private readonly string id;

        public CharacteristicColumn(string id)
        {
            this.id = id;
        }


        public string ColumnName => CharacteristicHelper.GetDisplayId(id);
        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => false;
        public ColumnCategory Category => ColumnCategory.Job;

        public bool IsDefault(Summary summary, Benchmark benchmark) => benchmark.Job.ToSet().Get(id).IsDefault;

        public string GetValue(Summary summary, Benchmark benchmark)
        {
            var characteristic = benchmark.Job.ToSet().Get(id);
            if (characteristic.IsDefault && EnvResolver.Instance.CanResolve(characteristic))
                return EnvResolver.Instance.Resolve(characteristic).ToString();
            return Presenter.ToPresentation(characteristic);
        }

        private static readonly Lazy<IColumn[]> LazyAllColumns =
            new Lazy<IColumn[]>(() => Job.Default.ToSet(false).GetValues().Select(c => new CharacteristicColumn(c.Id)).OfType<IColumn>().ToArray());

        public static IColumn[] AllColumns => LazyAllColumns.Value;
    }
}