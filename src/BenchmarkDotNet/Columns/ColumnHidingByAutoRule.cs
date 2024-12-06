using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Columns
{
    internal class ColumnHidingByAutoRule : IColumnHidingRule
    {
        public string RuleName { get; }

        public ColumnHidingByAutoRule(string ruleName) => RuleName = ruleName;

        public bool NeedToHide(IColumn column) => column.ColumnName == RuleName;
    }
}
