using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.IntegrationTests.ConfigPerAssembly
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class AssemblyConfigAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public static bool IsActivated;

        public AssemblyConfigAttribute()
        {
            Config = ManualConfig.CreateEmpty().With(new Logger());
        }

        private class Logger : ILogger
        {
            public void Write(LogKind logKind, string text)
            {
                IsActivated = true;
            }

            public void WriteLine()
            {
                IsActivated = true;
            }

            public void WriteLine(LogKind logKind, string text)
            {
                IsActivated = true;
            }
        }
    }
}