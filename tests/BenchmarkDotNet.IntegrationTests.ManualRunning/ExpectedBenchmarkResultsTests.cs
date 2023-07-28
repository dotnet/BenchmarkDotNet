using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Perfolizer.Horology;
using Perfolizer.Mathematics.Thresholds;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning
{
    public class ExpectedBenchmarkResultsTests : BenchmarkTestExecutor
    {
        // NativeAot takes a long time to build, so not including it in these tests.
        // We also don't test InProcessNoEmitToolchain because it is known to be less accurate than code-gen toolchains.

        private static readonly TimeInterval FallbackCpuResolutionValue = TimeInterval.FromNanoseconds(0.2d);

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

        [TheoryEnvSpecific("To not repeat tests in both Full .NET Framework and Core", EnvRequirement.DotNetCoreOnly)]
        [MemberData(nameof(CoreData))]
        public void EmptyBenchmarksReportZeroTimeAndAllocated_Core(Type benchmarkType, RuntimeMoniker runtimeMoniker)
        {
            AssertZeroResults(benchmarkType, ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithRuntime(runtimeMoniker.GetRuntime())
                ));
        }

        [TheoryEnvSpecific("Can only run Full .NET Framework and Mono tests from Framework host", EnvRequirement.FullFrameworkOnly)]
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
                .WithSummaryStyle(SummaryStyle.Default.WithTimeUnit(TimeUnit.Nanosecond))
                .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(false)))
            );

            var cpuResolution = RuntimeInformation.GetCpuInfo().MaxFrequency?.ToResolution() ?? FallbackCpuResolutionValue;
            var threshold = Threshold.Create(ThresholdUnit.Nanoseconds, cpuResolution.Nanoseconds);

            foreach (var report in summary.Reports)
            {
                var workloadMeasurements = report.AllMeasurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).GetStatistics().WithoutOutliers();
                var overheadMeasurements = report.AllMeasurements.Where(m => m.Is(IterationMode.Overhead, IterationStage.Actual)).GetStatistics().WithoutOutliers();

                bool isZero = ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(workloadMeasurements, overheadMeasurements, threshold);
                Assert.True(isZero, $"Actual time was not 0.");

                isZero = ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(overheadMeasurements, workloadMeasurements, threshold);
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

        [TheoryEnvSpecific("To not repeat tests in both Full .NET Framework and Core", EnvRequirement.DotNetCoreOnly)]
        [InlineData(RuntimeMoniker.Net70)]
        [InlineData(RuntimeMoniker.Mono70)]
        public void DifferentSizedStructsBenchmarksReportsNonZeroTimeAndZeroAllocated_Core(RuntimeMoniker runtimeMoniker)
        {
            AssertDifferentSizedStructsResults(ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithRuntime(runtimeMoniker.GetRuntime())
                ));
        }

        [TheoryEnvSpecific("Can only run Full .NET Framework and Mono tests from Framework host", EnvRequirement.FullFrameworkOnly)]
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
                .WithSummaryStyle(SummaryStyle.Default.WithTimeUnit(TimeUnit.Nanosecond))
                .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(false)))
            );

            var cpuResolution = RuntimeInformation.GetCpuInfo().MaxFrequency?.ToResolution() ?? FallbackCpuResolutionValue;
            var threshold = Threshold.Create(ThresholdUnit.Nanoseconds, cpuResolution.Nanoseconds);

            foreach (var report in summary.Reports)
            {
                var workloadMeasurements = report.AllMeasurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).GetStatistics().WithoutOutliers();
                var overheadMeasurements = report.AllMeasurements.Where(m => m.Is(IterationMode.Overhead, IterationStage.Actual)).GetStatistics().WithoutOutliers();

                bool isZero = ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(workloadMeasurements, overheadMeasurements, threshold);
                Assert.False(isZero, $"Actual time was 0.");

                isZero = ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(overheadMeasurements, workloadMeasurements, threshold);
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

    public class DifferentSizedStructs
    {
        [Benchmark] public Struct16 Struct16() => default;
        [Benchmark] public Struct32 Struct32() => default;
        [Benchmark] public Struct64 Struct64() => default;
        [Benchmark] public Struct128 Struct128() => default;
    }
}

public class EmptyVoid { [Benchmark] public void Void() { } }
public class EmptyByte { [Benchmark] public byte Byte() => default; }
public class EmptySByte { [Benchmark] public sbyte SByte() => default; }
public class EmptyShort { [Benchmark] public short Short() => default; }
public class EmptyUShort { [Benchmark] public ushort UShort() => default; }
public class EmptyChar { [Benchmark] public char Char() => default; }
public class EmptyInt32 { [Benchmark] public int Int32() => default; }
public class EmptyUInt32 { [Benchmark] public uint UInt32() => default; }
public class EmptyInt64 { [Benchmark] public long Int64() => default; }
public class EmptyUInt64 { [Benchmark] public ulong UInt64() => default; }
public class EmptyIntPtr { [Benchmark] public IntPtr IntPtr() => default; }
public class EmptyUIntPtr { [Benchmark] public UIntPtr UIntPtr() => default; }
public class EmptyVoidPointer { [Benchmark] public unsafe void* VoidPointer() => default; }
public class EmptyClass { [Benchmark] public object Class() => default; }