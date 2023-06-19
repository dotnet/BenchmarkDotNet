using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Perfolizer;
using Perfolizer.Horology;
using Perfolizer.Mathematics.Common;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.SignificanceTesting.MannWhitney;
using Perfolizer.Metrology;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning
{
    public class ExpectedBenchmarkResultsTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
    {
        // NativeAot takes a long time to build, so not including it in these tests.
        // We also don't test InProcessNoEmitToolchain because it is known to be less accurate than code-gen toolchains.

        private static readonly TimeInterval FallbackCpuResolutionValue = TimeInterval.FromNanoseconds(0.2d);

        private static IEnumerable<Type> EmptyBenchmarkTypes() =>
            [
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
            ];

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
                yield return new object[] { type, RuntimeMoniker.Net80 };
                yield return new object[] { type, RuntimeMoniker.Mono80 };
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
        public void EmptyBenchmarkReportsZeroTimeAndAllocated_InProcess(Type benchmarkType)
        {
            AssertZeroResults(benchmarkType, ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithToolchain(InProcessEmitToolchain.Instance)
                // IL Emit has incorrect overhead measurement. https://github.com/dotnet/runtime/issues/89685
                // We multiply the threshold to account for it.
                ), multiplyThresholdBy: RuntimeInformation.IsNetCore ? 3 : 1);
        }

        [TheoryEnvSpecific("To not repeat tests in both Full .NET Framework and Core", EnvRequirement.DotNetCoreOnly)]
        [MemberData(nameof(CoreData))]
        public void EmptyBenchmarkReportsZeroTimeAndAllocated_Core(Type benchmarkType, RuntimeMoniker runtimeMoniker)
        {
            AssertZeroResults(benchmarkType, ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithRuntime(runtimeMoniker.GetRuntime())
                ));
        }

        [TheoryEnvSpecific("Can only run Full .NET Framework and Mono tests from Framework host", EnvRequirement.FullFrameworkOnly)]
        [MemberData(nameof(FrameworkData))]
        public void EmptyBenchmarkReportsZeroTimeAndAllocated_Framework(Type benchmarkType, RuntimeMoniker runtimeMoniker)
        {
            AssertZeroResults(benchmarkType, ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithRuntime(runtimeMoniker.GetRuntime())
                ));
        }

        private void AssertZeroResults(Type benchmarkType, IConfig config, int multiplyThresholdBy = 1)
        {
            var summary = CanExecute(benchmarkType, config
                .WithSummaryStyle(SummaryStyle.Default.WithTimeUnit(TimeUnit.Nanosecond))
                .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(false)))
            );

            var cpuResolution = CpuDetector.Cpu?.MaxFrequency()?.ToResolution() ?? FallbackCpuResolutionValue;
            var threshold = new NumberValue(cpuResolution.Nanoseconds * multiplyThresholdBy).ToThreshold();

            foreach (var report in summary.Reports)
            {
                var workloadMeasurements = report.AllMeasurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).GetStatistics().Sample;
                var overheadMeasurements = report.AllMeasurements.Where(m => m.Is(IterationMode.Overhead, IterationStage.Actual)).GetStatistics().Sample;

                bool isZero = ZeroMeasurementHelper.AreIndistinguishable(workloadMeasurements, overheadMeasurements, threshold);
                Assert.True(isZero, $"Actual time was not 0.");

                Assert.True((report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase) ?? 0L) == 0L, "Memory allocations measured above 0.");
            }
        }

        private static IEnumerable<Type> NonEmptyBenchmarkTypes() =>
            [
                typeof(DifferentSizedStructs),
                typeof(ActualWork)
            ];

        public static IEnumerable<object[]> NonEmptyInProcessData()
        {
            foreach (var type in NonEmptyBenchmarkTypes())
            {
                yield return new object[] { type };
            }
        }

        public static IEnumerable<object[]> NonEmptyCoreData()
        {
            foreach (var type in NonEmptyBenchmarkTypes())
            {
                yield return new object[] { type, RuntimeMoniker.Net80 };
                yield return new object[] { type, RuntimeMoniker.Mono80 };
            }
        }

        public static IEnumerable<object[]> NonEmptyFrameworkData()
        {
            foreach (var type in NonEmptyBenchmarkTypes())
            {
                yield return new object[] { type, RuntimeMoniker.Net462 };
                yield return new object[] { type, RuntimeMoniker.Mono };
            }
        }

        [Theory]
        [MemberData(nameof(NonEmptyInProcessData))]
        public void NonEmptyBenchmarkReportsNonZeroTimeAndZeroAllocated_InProcess(Type benchmarkType)
        {
            AssertNonZeroResults(benchmarkType, ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithToolchain(InProcessEmitToolchain.Instance)
                // InProcess overhead measurements are incorrect, so we adjust the results to account for it. https://github.com/dotnet/runtime/issues/89685
                ), subtractOverheadByClocks: RuntimeInformation.IsNetCore ? 3 : 1);
        }

        [TheoryEnvSpecific("To not repeat tests in both Full .NET Framework and Core", EnvRequirement.DotNetCoreOnly)]
        [MemberData(nameof(NonEmptyCoreData))]
        public void NonEmptyBenchmarkReportsNonZeroTimeAndZeroAllocated_Core(Type benchmarkType, RuntimeMoniker runtimeMoniker)
        {
            AssertNonZeroResults(benchmarkType, ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithRuntime(runtimeMoniker.GetRuntime())
                ));
        }

        [TheoryEnvSpecific("Can only run Mono tests from Framework host", EnvRequirement.FullFrameworkOnly)]
        [MemberData(nameof(NonEmptyFrameworkData))]
        public void NonEmptyBenchmarkReportsNonZeroTimeAndZeroAllocated_Framework(Type benchmarkType, RuntimeMoniker runtimeMoniker)
        {
            AssertNonZeroResults(benchmarkType, ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithRuntime(runtimeMoniker.GetRuntime())
                ));
        }

        private void AssertNonZeroResults(Type benchmarkType, IConfig config, int subtractOverheadByClocks = 0)
        {
            var summary = CanExecute(benchmarkType, config
                .WithSummaryStyle(SummaryStyle.Default.WithTimeUnit(TimeUnit.Nanosecond))
                .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(false)))
            );

            var cpuResolution = CpuDetector.Cpu?.MaxFrequency()?.ToResolution() ?? FallbackCpuResolutionValue;
            // Modern cpus can execute multiple instructions per clock cycle,
            // resulting in measurements greater than 0 but less than 1 clock cycle.
            // (example: Intel Core i9-9880H CPU 2.30GHz reports 0.2852 ns for `_field++;`)
            var threshold = new NumberValue(cpuResolution.Nanoseconds / 4).ToThreshold();
            // InProcess overhead measurements are incorrect, so we adjust the results to account for it. https://github.com/dotnet/runtime/issues/89685
            var overheadSubtraction = cpuResolution.Nanoseconds * subtractOverheadByClocks;

            foreach (var report in summary.Reports)
            {
                var workloadMeasurements = report.AllMeasurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).GetStatistics().Sample;
                var overheadMeasurements = new Sample(report.AllMeasurements
                    .Where(m => m.Is(IterationMode.Overhead, IterationStage.Actual))
                    .GetStatistics().OriginalValues
                    .Select(x => x - overheadSubtraction).ToArray());

                var comparisonResult = new SimpleEquivalenceTest(MannWhitneyTest.Instance).Perform(workloadMeasurements, overheadMeasurements, threshold, SignificanceLevel.P1E5);
                Assert.True(comparisonResult == ComparisonResult.Greater, "Workload measurements are not greater than overhead.");

                Assert.True((report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase) ?? 0L) == 0L, "Memory allocations measured above 0.");
            }
        }
    }
}

// Types outside of namespace so it's easier to read in the test explorer.
#pragma warning disable CA1050 // Declare types in namespaces
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
    public long l1, l2, l3, l4, l5, l6, l7, l8;
}

public struct Struct128
{
    public long l1, l2, l3, l4, l5, l6, l7, l8, l9, l10, l11, l12, l13, l14, l15, l16;
}

public class DifferentSizedStructs
{
    [Benchmark] public Struct16 Struct16() => default;
    [Benchmark] public Struct32 Struct32() => default;
    [Benchmark] public Struct64 Struct64() => default;
    [Benchmark] public Struct128 Struct128() => default;
}

public class ActualWork
{
    public int _field;

    [Benchmark]
    public void IncrementField() => _field++;
}

public class EmptyVoid
{
    [Benchmark] public void Void() { }
}

public class EmptyByte
{
    [Benchmark] public byte Byte() => default;
}

public class EmptySByte
{
    [Benchmark] public sbyte SByte() => default;
}

public class EmptyShort
{
    [Benchmark] public short Short() => default;
}

public class EmptyUShort
{
    [Benchmark] public ushort UShort() => default;
}

public class EmptyChar
{
    [Benchmark] public char Char() => default;
}

public class EmptyInt32
{
    [Benchmark] public int Int32() => default;
}

public class EmptyUInt32
{
    [Benchmark] public uint UInt32() => default;
}

public class EmptyInt64
{
    [Benchmark] public long Int64() => default;
}

public class EmptyUInt64
{
    [Benchmark] public ulong UInt64() => default;
}

public class EmptyIntPtr
{
    [Benchmark] public IntPtr IntPtr() => default;
}

public class EmptyUIntPtr
{
    [Benchmark] public UIntPtr UIntPtr() => default;
}

public class EmptyVoidPointer
{
    [Benchmark] public unsafe void* VoidPointer() => default;
}

public class EmptyClass
{
    [Benchmark] public object Class() => default;
}