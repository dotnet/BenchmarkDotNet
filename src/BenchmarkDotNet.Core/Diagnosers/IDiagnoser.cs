﻿using System.Diagnostics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Diagnosers
{
    public interface IDiagnoser
    {
        IColumnProvider GetColumnProvider();

        /// <summary>
        /// before jitting, warmup
        /// </summary>
        void BeforeAnythingElse(Process process, Benchmark benchmark);

        /// <summary>
        /// after setup, before run
        /// </summary>
        void AfterSetup(Process process, Benchmark benchmark);

        /// <summary>
        /// after run, before cleanup
        /// </summary>
        void BeforeCleanup();

        void ProcessResults(Benchmark benchmark, BenchmarkReport report);

        void DisplayResults(ILogger logger);
    }
}
