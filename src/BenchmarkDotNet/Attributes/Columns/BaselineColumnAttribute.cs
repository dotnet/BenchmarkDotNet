using System;
using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class BaselineColumnAttribute : ColumnConfigBaseAttribute
    {
        public BaselineColumnAttribute() : base(BaselineColumn.Default) { }
    }
}