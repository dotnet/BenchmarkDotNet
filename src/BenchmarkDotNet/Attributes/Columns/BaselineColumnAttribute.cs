using System;
using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class BaselineColumnAttribute : ColumnConfigBaseAttribute
    {
        public BaselineColumnAttribute() : base(BaselineColumn.Default) { }
    }
}