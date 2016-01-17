using BenchmarkDotNet.Reports;
using System;
using System.Collections.Generic;
using BenchmarkDotNet.Common;
using BenchmarkDotNet.Statistic;

namespace BenchmarkDotNet.Plugins.ResultExtenders
{
    public interface IBenchmarkResultExtender
    {
        string ColumnName { get; }

        /// <summary>
        /// It is expected that GetExtendedResults will return an <code>IList{string}</code> with the same amount
        /// of items as the <code>IList{Tuple{BenchmarkReport, StatSummary}}</code> that is passed to it
        /// </summary>
        /// <param name="reports"></param>
        /// <returns>null, if extender is invalid for this benchmark set. Otherwise, a valid list of string.</returns>
        IList<string> GetExtendedResults(IList<Tuple<BenchmarkReport, StatSummary>> reports, TimeUnit timeUnit);
    }
}
