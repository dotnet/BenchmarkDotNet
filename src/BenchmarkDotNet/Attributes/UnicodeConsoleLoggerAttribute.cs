using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Enable unicode support in console logger
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class UnicodeConsoleLoggerAttribute : Attribute, IConfigSource
    {
        public UnicodeConsoleLoggerAttribute()
        {
            Config = ManualConfig.CreateEmpty().AddLogger(ConsoleLogger.Unicode);
        }

        public IConfig Config { get; }
    }
}