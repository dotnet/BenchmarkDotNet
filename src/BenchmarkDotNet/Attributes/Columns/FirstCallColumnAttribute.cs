using System;
using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{   
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class FirstCallColumnAttribute : ColumnConfigBaseAttribute
    {
        public FirstCallColumnAttribute() : base(FirstCallColumn.Default) { }
    }
}