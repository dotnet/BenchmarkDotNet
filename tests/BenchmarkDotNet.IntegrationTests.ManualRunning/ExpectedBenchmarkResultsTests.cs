using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Perfolizer.Horology;
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

            var cpuResolution = RuntimeInformation.GetCpuInfo().MaxFrequency?.ToResolution() ?? FallbackCpuResolutionValue;
            var cpuGhz = cpuResolution.ToFrequency().ToGHz();

            foreach (var report in summary.Reports)
            {
                var workloadTimes = report.AllMeasurements
                    .Where(m => m.IsOverhead() && m.IterationStage == Engines.IterationStage.Actual)
                    .Select(m => m.GetAverageTime().Nanoseconds)
                    .ToArray();
                var overheadTimes = report.AllMeasurements
                    .Where(m => m.IsOverhead() && m.IterationStage == Engines.IterationStage.Actual)
                    .Select(m => m.GetAverageTime().Nanoseconds)
                    .ToArray();

                bool isZero = ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(workloadTimes, overheadTimes);
                Assert.True(isZero, $"Actual time was not 0.");

                var workloadTime = workloadTimes.Average();
                var overheadTime = overheadTimes.Average();

                // Allow for 1 cpu cycle variance
                Assert.True(overheadTime * cpuGhz < workloadTime * cpuGhz + 1, "Overhead took more time than workload.");

                Assert.True((report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase) ?? 0L) == 0L, "Memory allocations measured above 0.");
            }
        }

        [Fact]
        public void LargeStructBenchmarksReportsNonZeroTimeAndZeroAllocated_InProcess()
        {
            AssertLargeStructResults(ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithToolchain(InProcessEmitToolchain.Instance)
                ));
        }

        [TheoryNetCoreOnly("To not repeat tests in both full Framework and Core")]
        [InlineData(RuntimeMoniker.Net70)]
        [InlineData(RuntimeMoniker.Mono70)]
        public void LargeStructBenchmarksReportsNonZeroTimeAndZeroAllocated_Core(RuntimeMoniker runtimeMoniker)
        {
            AssertLargeStructResults(ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithRuntime(runtimeMoniker.GetRuntime())
                ));
        }

        [TheoryFullFrameworkOnly("Can only run full Framework and Mono tests from Framework host")]
        [InlineData(RuntimeMoniker.Net462)]
        [InlineData(RuntimeMoniker.Mono)]
        public void LargeStructBenchmarksReportsNonZeroTimeAndZeroAllocated_Framework(RuntimeMoniker runtimeMoniker)
        {
            AssertLargeStructResults(ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithRuntime(runtimeMoniker.GetRuntime())
                ));
        }

        private void AssertLargeStructResults(IConfig config)
        {
            var summary = CanExecute<LargeStruct>(config
                .WithSummaryStyle(SummaryStyle.Default.WithTimeUnit(Perfolizer.Horology.TimeUnit.Nanosecond))
                .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(false)))
            );

            var cpuResolution = RuntimeInformation.GetCpuInfo().MaxFrequency?.ToResolution() ?? FallbackCpuResolutionValue;
            var cpuGhz = cpuResolution.ToFrequency().ToGHz();

            foreach (var report in summary.Reports)
            {
                var workloadTimes = report.AllMeasurements
                    .Where(m => m.IsOverhead() && m.IterationStage == Engines.IterationStage.Actual)
                    .Select(m => m.GetAverageTime().Nanoseconds)
                    .ToArray();
                var overheadTimes = report.AllMeasurements
                    .Where(m => m.IsOverhead() && m.IterationStage == Engines.IterationStage.Actual)
                    .Select(m => m.GetAverageTime().Nanoseconds)
                    .ToArray();

                bool isZero = ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(workloadTimes, overheadTimes);
                Assert.False(isZero, $"Actual time was 0.");

                var workloadTime = workloadTimes.Average();
                var overheadTime = overheadTimes.Average();

                // Allow for 1 cpu cycle variance
                Assert.True(overheadTime * cpuGhz < workloadTime * cpuGhz + 1, "Overhead took more time than workload.");

                Assert.True((report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase) ?? 0L) == 0L, "Memory allocations measured above 0.");
            }
        }
    }

    public class LargeStruct
    {
        public struct Struct
        {
            // 128 bits
            public long l1, l2, l3, l4,
                        l5, l6, l7, l8,
                        l9, l10, l11, l12,
                        l13, l14, l15, l16;
        }

        [Benchmark] public Struct Benchmark() => default;
    }
}

public class EmptyVoid { [Benchmark] public void Benchmark() { } }
public class EmptyByte { [Benchmark] public byte Benchmark() => default; }
public class EmptySByte { [Benchmark] public sbyte Benchmark() => default; }
public class EmptyShort { [Benchmark] public short Benchmark() => default; }
public class EmptyUShort { [Benchmark] public ushort Benchmark() => default; }
public class EmptyChar { [Benchmark] public char Benchmark() => default; }
public class EmptyInt32 { [Benchmark] public int Benchmark() => default; }
public class EmptyUInt32 { [Benchmark] public uint Benchmark() => default; }
public class EmptyInt64 { [Benchmark] public long Benchmark() => default; }
public class EmptyUInt64 { [Benchmark] public ulong Benchmark() => default; }
public class EmptyIntPtr { [Benchmark] public IntPtr Benchmark() => default; }
public class EmptyUIntPtr { [Benchmark] public UIntPtr Benchmark() => default; }
public class EmptyVoidPointer { [Benchmark] public unsafe void* Benchmark() => default; }
public class EmptyClass { [Benchmark] public object Class() => default; }