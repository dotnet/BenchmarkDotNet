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
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Perfolizer.Horology;
using Perfolizer.Mathematics.Common;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.SignificanceTesting.MannWhitney;
using Perfolizer.Metrology;
using Pragmastat;
using Pragmastat.Metrology;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning
{
    public class ExpectedBenchmarkResultsTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
    {
        private static readonly TimeInterval FallbackCpuResolutionValue = TimeInterval.FromNanoseconds(0.2d);

        // Visual Studio Test Explorer doesn't like to display IToolchain params separately, so use an enum instead.
        public enum ToolchainType
        {
            Default,
            InProcess
        }

        private IToolchain GetToolchain(ToolchainType toolchain)
            => toolchain switch
            {
                ToolchainType.Default => Job.Default.GetToolchain(),
                ToolchainType.InProcess => InProcessEmitToolchain.Default,
                _ => throw new ArgumentOutOfRangeException()
            };

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

        public static IEnumerable<object[]> GetEmptyArgs()
        {
            foreach (var type in EmptyBenchmarkTypes())
            {
                yield return new object[] { ToolchainType.Default, type };
                // InProcess overhead measurements are incorrect in Core. https://github.com/dotnet/runtime/issues/89685
                if (!RuntimeInformation.IsNetCore)
                {
                    yield return new object[] { ToolchainType.InProcess, type };
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetEmptyArgs))]
        public void EmptyBenchmarkReportsZeroTimeAndAllocated(ToolchainType toolchain, Type benchmarkType)
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Default.WithToolchain(GetToolchain(toolchain)))
                .WithSummaryStyle(SummaryStyle.Default.WithTimeUnit(TimeUnit.Nanosecond))
                .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(false)));

            var summary = CanExecute(benchmarkType, config);

            var cpuResolution = CpuDetector.Cpu?.MaxFrequency()?.ToResolution() ?? FallbackCpuResolutionValue;
            var threshold = new NumberValue(cpuResolution.Nanoseconds).ToThreshold();

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
                // Structs even as large as Struct128 results in zero measurements on Zen 5, so the test will only pass on older or different CPU architectures.
                //typeof(DifferentSizedStructs),
                typeof(ActualWork)
            ];

        public static IEnumerable<object[]> GetNonEmptyArgs()
        {
            foreach (var type in NonEmptyBenchmarkTypes())
            {
                // Framework is slightly less accurate than Core.
                yield return new object[] { ToolchainType.Default, type, RuntimeInformation.IsNetCore ? 0 : 1 };
                // InProcess overhead measurements are incorrect in Core. https://github.com/dotnet/runtime/issues/89685
                if (!RuntimeInformation.IsNetCore)
                {
                    yield return new object[] { ToolchainType.InProcess, type, 1 };
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetNonEmptyArgs))]
        public void NonEmptyBenchmarkReportsNonZeroTimeAndZeroAllocated(ToolchainType toolchain, Type benchmarkType, int subtractOverheadByClocks)
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Default.WithToolchain(GetToolchain(toolchain)))
                .WithSummaryStyle(SummaryStyle.Default.WithTimeUnit(TimeUnit.Nanosecond))
                .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(false)));

            var summary = CanExecute(benchmarkType, config);

            var cpuResolution = CpuDetector.Cpu?.MaxFrequency()?.ToResolution() ?? FallbackCpuResolutionValue;
            // Modern cpus can execute multiple instructions per clock cycle,
            // resulting in measurements greater than 0 but less than 1 clock cycle.
            // (example: Intel Core i9-9880H CPU 2.30GHz reports 0.2852 ns for `_field++;`)
            var threshold = Threshold.Zero;
            var overheadSubtraction = cpuResolution.Nanoseconds * subtractOverheadByClocks;

            foreach (var report in summary.Reports)
            {
                var workloadMeasurements = report.AllMeasurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).GetStatistics().Sample;
                var overheadMeasurements = report.AllMeasurements
                    .Where(m => m.Is(IterationMode.Overhead, IterationStage.Actual))
                    .Select(x => new Reports.Measurement(x.LaunchIndex, x.IterationMode, x.IterationStage, x.IterationIndex, x.Operations, x.Nanoseconds - overheadSubtraction))
                    .GetStatistics().Sample;

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
    [Benchmark] public object? Class() => default;
}