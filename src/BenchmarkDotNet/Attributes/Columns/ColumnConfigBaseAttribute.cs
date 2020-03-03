using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public abstract class ColumnConfigBaseAttribute : Attribute, IConfigSource
    {
        // CLS-Compliant Code requires a constructor without an array in the argument list
        [PublicAPI]
        protected ColumnConfigBaseAttribute()
        {
            Config = ManualConfig.CreateEmpty();
        }

        protected ColumnConfigBaseAttribute(params IColumn[] columns)
        {
            Config = ManualConfig.CreateEmpty().AddColumn(columns);
        }

        public IConfig Config { get; }
    }
}