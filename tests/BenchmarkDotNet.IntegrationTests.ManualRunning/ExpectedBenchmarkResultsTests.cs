﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning
{
    public class ExpectedBenchmarkResultsTests : BenchmarkTestExecutor
    {
        // NativeAot takes a long time to build, so not including it in these tests.
        // We also don't test InProcessNoEmitToolchain because it is known to be less accurate than code-gen toolchains.

        public ExpectedBenchmarkResultsTests(ITestOutputHelper output) : base(output) { }

        private static IEnumerable<Type> EmptyBenchmarkTypes() =>
            new[]
            {
                typeof(EmptyVoid),
                typeof(EmptyByte),
                typeof(EmptySByte),
                typeof(EmptyShort),
                typeof(EmptyUShort),
                typeof(EmptyChar),
                typeof(EmptyInt32),
                typeof(EmptyUInt32),
                typeof(EmptyInt64),
                typeof(EmptyUInt64),
                typeof(EmptyIntPtr),
                typeof(EmptyUIntPtr),
                typeof(EmptyVoidPointer),
                typeof(EmptyClass)
            };

        public static IEnumerable<object[]> InProcessData()
        {
            foreach (var type in EmptyBenchmarkTypes())
            {
                yield return new object[] { type };
            }
        }

        public static IEnumerable<object[]> CoreData()
        {
            foreach (var type in EmptyBenchmarkTypes())
            {
                yield return new object[] { type, RuntimeMoniker.Net70 };
                yield return new object[] { type, RuntimeMoniker.Mono70 };
            }
        }

        public static IEnumerable<object[]> FrameworkData()
        {
            foreach (var type in EmptyBenchmarkTypes())
            {
                yield return new object[] { type, RuntimeMoniker.Net462 };
                yield return new object[] { type, RuntimeMoniker.Mono };
            }
        }

        [Theory]
        [MemberData(nameof(InProcessData))]
        public void EmptyBenchmarksReportZeroTimeAndAllocated_InProcess(Type benchmarkType)
        {
            AssertZeroResults(benchmarkType, ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithToolchain(InProcessEmitToolchain.Instance)
                ));
        }

        [TheoryNetCoreOnly("To not repeat tests in both full Framework and Core")]
        [MemberData(nameof(CoreData))]
        public void EmptyBenchmarksReportZeroTimeAndAllocated_Core(Type benchmarkType, RuntimeMoniker runtimeMoniker)
        {
            AssertZeroResults(benchmarkType, ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithRuntime(runtimeMoniker.GetRuntime())
                ));
        }

        [TheoryFullFrameworkOnly("Can only run full Framework and Mono tests from Framework host")]
        [MemberData(nameof(FrameworkData))]
        public void EmptyBenchmarksReportZeroTimeAndAllocated_Framework(Type benchmarkType, RuntimeMoniker runtimeMoniker)
        {
            AssertZeroResults(benchmarkType, ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithRuntime(runtimeMoniker.GetRuntime())
                ));
        }

        private void AssertZeroResults(Type benchmarkType, IConfig config)
        {
            var summary = CanExecute(benchmarkType, config
                .WithSummaryStyle(SummaryStyle.Default.WithTimeUnit(Perfolizer.Horology.TimeUnit.Nanosecond))
                .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(false)))
            );

            foreach (var report in summary.Reports)
            {
                var workloadMeasurements = report.AllMeasurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).GetStatistics().WithoutOutliers();
                var overheadMeasurements = report.AllMeasurements.Where(m => m.Is(IterationMode.Overhead, IterationStage.Actual)).GetStatistics().WithoutOutliers();

                bool isZero = ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(workloadMeasurements, overheadMeasurements);
                Assert.True(isZero, $"Actual time was not 0.");

                isZero = ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(overheadMeasurements, workloadMeasurements);
                Assert.True(isZero, "Overhead took more time than workload.");

                Assert.True((report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase) ?? 0L) == 0L, "Memory allocations measured above 0.");
            }
        }

        [Fact]
        public void DifferentSizedStructsBenchmarksReportsNonZeroTimeAndZeroAllocated_InProcess()
        {
            AssertDifferentSizedStructsResults(ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithToolchain(InProcessEmitToolchain.Instance)
                ));
        }

        [TheoryNetCoreOnly("To not repeat tests in both full Framework and Core")]
        [InlineData(RuntimeMoniker.Net70)]
        [InlineData(RuntimeMoniker.Mono70)]
        public void DifferentSizedStructsBenchmarksReportsNonZeroTimeAndZeroAllocated_Core(RuntimeMoniker runtimeMoniker)
        {
            AssertDifferentSizedStructsResults(ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithRuntime(runtimeMoniker.GetRuntime())
                ));
        }

        [TheoryFullFrameworkOnly("Can only run full Framework and Mono tests from Framework host")]
        [InlineData(RuntimeMoniker.Net462)]
        [InlineData(RuntimeMoniker.Mono)]
        public void DifferentSizedStructsBenchmarksReportsNonZeroTimeAndZeroAllocated_Framework(RuntimeMoniker runtimeMoniker)
        {
            AssertDifferentSizedStructsResults(ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithRuntime(runtimeMoniker.GetRuntime())
                ));
        }

        private void AssertDifferentSizedStructsResults(IConfig config)
        {
            var summary = CanExecute<DifferentSizedStructs>(config
                .WithSummaryStyle(SummaryStyle.Default.WithTimeUnit(Perfolizer.Horology.TimeUnit.Nanosecond))
                .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(false)))
            );

            foreach (var report in summary.Reports)
            {
                var workloadMeasurements = report.AllMeasurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).GetStatistics().WithoutOutliers();
                var overheadMeasurements = report.AllMeasurements.Where(m => m.Is(IterationMode.Overhead, IterationStage.Actual)).GetStatistics().WithoutOutliers();

                bool isZero = ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(workloadMeasurements, overheadMeasurements);
                Assert.False(isZero, $"Actual time was 0.");

                isZero = ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(overheadMeasurements, workloadMeasurements);
                Assert.True(isZero, "Overhead took more time than workload.");

                Assert.True((report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase) ?? 0L) == 0L, "Memory allocations measured above 0.");
            }
        }
    }

    public struct Struct16
    {
        public long l1, l2;
    }

    public struct Struct32
    {
        public long l1, l2, l3, l4;
    }

    public struct Struct64
    {
        public long l1, l2, l3, l4,
                    l5, l6, l7, l8;
    }

    public struct Struct128
    {
        public long l1, l2, l3, l4,
                    l5, l6, l7, l8,
                    l9, l10, l11, l12,
                    l13, l14, l15, l16;
    }

    public class DifferentSizedStructs : RealTimeBenchmarks
    {
        [Benchmark] public Struct16 Struct16() => default;
        [Benchmark] public Struct32 Struct32() => default;
        [Benchmark] public Struct64 Struct64() => default;
        [Benchmark] public Struct128 Struct128() => default;
    }
}

public class RealTimeBenchmarks
{
    private Process process;
    private ProcessPriorityClass oldPriority;

    [GlobalSetup]
    public void Setup()
    {
        process = Process.GetCurrentProcess();
        try
        {
            oldPriority = process.PriorityClass;
            // Requires admin mode. Makes the OS never give up CPU time for this process, so we can get more accurate timings.
            process.PriorityClass = ProcessPriorityClass.RealTime;
        }
        catch (PlatformNotSupportedException) { }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try
        {
            process.PriorityClass = oldPriority;
        }
        catch (PlatformNotSupportedException) { }
    }
}

public class EmptyVoid : RealTimeBenchmarks { [Benchmark] public void Benchmark() { } }
public class EmptyByte : RealTimeBenchmarks { [Benchmark] public byte Benchmark() => default; }
public class EmptySByte : RealTimeBenchmarks { [Benchmark] public sbyte Benchmark() => default; }
public class EmptyShort : RealTimeBenchmarks { [Benchmark] public short Benchmark() => default; }
public class EmptyUShort : RealTimeBenchmarks { [Benchmark] public ushort Benchmark() => default; }
public class EmptyChar : RealTimeBenchmarks { [Benchmark] public char Benchmark() => default; }
public class EmptyInt32 : RealTimeBenchmarks { [Benchmark] public int Benchmark() => default; }
public class EmptyUInt32 : RealTimeBenchmarks { [Benchmark] public uint Benchmark() => default; }
public class EmptyInt64 : RealTimeBenchmarks { [Benchmark] public long Benchmark() => default; }
public class EmptyUInt64 : RealTimeBenchmarks { [Benchmark] public ulong Benchmark() => default; }
public class EmptyIntPtr : RealTimeBenchmarks { [Benchmark] public IntPtr Benchmark() => default; }
public class EmptyUIntPtr : RealTimeBenchmarks { [Benchmark] public UIntPtr Benchmark() => default; }
public class EmptyVoidPointer : RealTimeBenchmarks { [Benchmark] public unsafe void* Benchmark() => default; }
public class EmptyClass : RealTimeBenchmarks { [Benchmark] public object Class() => default; }