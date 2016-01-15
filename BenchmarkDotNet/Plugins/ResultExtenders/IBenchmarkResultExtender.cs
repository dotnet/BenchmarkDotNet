using BenchmarkDotNet.Reports;
using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Plugins.ResultExtenders
{
    public interface IBenchmarkResultExtender
    {
        string ColumnName { get; }

        /// <summary>
        /// It is expected that GetExtendedResults will return an <code>IList{string}</code> with the same amount
        /// of items as the <code>IList{Tuple{BenchmarkReport, BenchmarkRunReportsStatistic}}</code> that is passed to it
        /// </summary>
        /// <param name="reports"></param>
        /// <returns></returns>
        IList<string> GetExtendedResults(IList<Tuple<BenchmarkReport, BenchmarkRunReportsStatistic>> reports);
    }
}
