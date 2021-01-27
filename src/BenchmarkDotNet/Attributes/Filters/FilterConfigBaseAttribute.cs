using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    public abstract class FilterConfigBaseAttribute : Attribute, IConfigSource
    {
        // CLS-Compliant Code requires a constructor without an array in the argument list
        protected FilterConfigBaseAttribute()
        {
            Config = ManualConfig.CreateEmpty();
        }

        protected FilterConfigBaseAttribute(params IFilter[] filters)
        {
            Config = ManualConfig.CreateEmpty().AddFilter(filters);
        }

        public IConfig Config { get; }
    }
}