using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Attributes.Columns
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public abstract class ColumnConfigAttribute : Attribute, IConfigSource
    {
        protected ColumnConfigAttribute(IColumn column)
        {
            Config = ManualConfig.CreateEmpty().With(column);
        }

        public IConfig Config { get; }
    }
}