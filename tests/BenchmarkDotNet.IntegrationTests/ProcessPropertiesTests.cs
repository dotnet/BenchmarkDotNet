﻿using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ProcessPropertiesTests : BenchmarkTestExecutor
    {
        public ProcessPropertiesTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [FactWindowsOnly("Process.set_PriorityClass requires root on Unix")]
        public void HighPriorityIsSet()
        {
            CanExecute<HighPriority>();
        }

        [FactWindowsOnly("Process.set_ProcessorAffinity requires root on Unix")]
        public void CustomAffinityCanBeSet()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithAffinity(CustomAffinity.Value))
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddLogger(new OutputLogger(Output));

            CanExecute<CustomAffinity>(config);
        }
    }

    public class HighPriority
    {
        [Benchmark]
        public void Ensure()
        {
            if (Process.GetCurrentProcess().PriorityClass != ProcessPriorityClass.High)
            {
                throw new InvalidOperationException("Did not set high priority");
            }
        }
    }

    public class CustomAffinity
    {
        public static readonly IntPtr Value = new IntPtr(2);

        [Benchmark]
        public void Ensure()
        {
            if (Process.GetCurrentProcess().ProcessorAffinity != Value)
            {
                throw new InvalidOperationException("Did not set custom affinity");
            }
        }
    }

}