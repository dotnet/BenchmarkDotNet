using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public interface IColumn
    {
        /// <summary>
        /// An unique identificator of the column.
        /// <remarks>If there are several columns with the same Id, only one of them will be shown in the summary.</remarks>
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Display column title in the summary.
        /// </summary>
        string ColumnName { get; }

        /// <summary>
        /// Column title formatted using the specified style.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        string GetName(ISummaryStyle style);

        /// <summary>
        /// Value in this column formatted using the default style.
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="benchmark"></param>
        /// <returns></returns>
        string GetValue(Summary summary, Benchmark benchmark);

        /// <summary>
        /// Value in this column formatted using the specified style.
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="benchmark"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style);

        bool IsDefault(Summary summary, Benchmark benchmark);

        bool IsAvailable(Summary summary);

        bool AlwaysShow { get; }

        ColumnCategory Category { get; }

        /// <summary>
        /// Defines order of column in the same category.
        /// </summary>
        int PriorityInCategory { get; }

        QuantityType QuantityType { get; }
    }
}
