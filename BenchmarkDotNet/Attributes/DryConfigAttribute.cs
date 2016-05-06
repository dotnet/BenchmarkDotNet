using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// This attribute has the same effect as writing <code>[Config("Jobs=Dry")]</code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class DryConfigAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public DryConfigAttribute()
        {
            Config = new ConfigParser().Parse(new[] { "Jobs=Dry" });
        }
    }
}
