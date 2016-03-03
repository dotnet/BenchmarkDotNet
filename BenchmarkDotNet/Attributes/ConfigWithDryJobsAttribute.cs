using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Attributes
{
    // This is here to save people having to write "[Config("Jobs=Dry")]" every time, i.e. less "magic strings"
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigWithDryJobsAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public ConfigWithDryJobsAttribute()
        {
            Config = new ConfigParser().Parse(new[] { "Jobs=Dry" });
        }
    }
}
