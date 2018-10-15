using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkRunInfo
    {
        public BenchmarkRunInfo(BenchmarkCase[] benchmarksCase, Type type, FinalConfig config)
        {
            BenchmarksCases = benchmarksCase;
            Type = type;
            Config = config;
        }

        public BenchmarkCase[] BenchmarksCases { get; }
        public Type Type { get; }
        public FinalConfig Config { get; }
    }
}