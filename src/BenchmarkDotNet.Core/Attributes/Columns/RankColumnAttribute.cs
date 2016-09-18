using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Mathematics;

namespace BenchmarkDotNet.Attributes.Columns
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class RankColumnAttribute : ColumnConfigBaseAttribute
    {
        public RankColumnAttribute(NumeralSystem system = NumeralSystem.Arabic) : base(new RankColumn(system))
        {
        }
    }
}