using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Disassemblers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class DisassemblyDiagnoserTests : BenchmarkTestExecutor
    {
        public DisassemblyDiagnoserTests(ITestOutputHelper output) : base(output) { }

        public static IEnumerable<object[]> GetAllJits()
        {
            if (RuntimeInformation.IsFullFramework)
            {
                yield return new object[] { Jit.LegacyJit, Platform.X86, ClrRuntime.Net462 }; // 32bit LegacyJit for desktop .NET
                yield return new object[] { Jit.LegacyJit, Platform.X64, ClrRuntime.Net462 }; // 64bit LegacyJit for desktop .NET
                yield return new object[] { Jit.RyuJit, Platform.X64, ClrRuntime.Net462 }; // RyuJit for desktop .NET
            }
            else if (RuntimeInformation.IsNetCore)
            {
                if (RuntimeInformation.GetCurrentPlatform() is Platform.X86 or Platform.X64)
                {
                    yield return new object[] { Jit.RyuJit, Platform.X64, CoreRuntime.Core80 }; // .NET Core x64
                }
                else if (RuntimeInformation.GetCurrentPlatform() is Platform.Arm64 && RuntimeInformation.IsLinux())
                {
                    yield return new object[] { Jit.RyuJit, Platform.Arm64, CoreRuntime.Core80 }; // .NET Core arm64
                }
            }
            if (RuntimeInformation.IsMacOS())
            {
                // This scope of tests is not supported on macOS
                // However, when the MemberData method provides no data, xUnit throws an "No data found" InvalidOperationException
                // In order to fix the problem, we should provide at least one input data set
                // All the tests check the OS on the first line and stop the test if it's macOS
                yield return new object[] { Jit.Default, Platform.AnyCpu, CoreRuntime.Latest };
            }

            // we could add new object[] { Jit.Llvm, Platform.X64, new MonoRuntime() } here but our CI would need to have Mono installed..
        }

        public class WithCalls
        {
            [Benchmark]
            [Arguments(int.MaxValue)]
            public void Benchmark(int someArgument)
            {
                if (someArgument != int.MaxValue)
                    throw new InvalidOperationException("Wrong value of the argument!!");

                // we should rather have test per use case
                // but running so many tests for all JITs would take too much time
                // so we have one method that does it all
                Static();
                Instance();
                Recursive();

                Benchmark(true);
            }

            [MethodImpl(MethodImplOptions.NoInlining)] public static void Static() { }

            [MethodImpl(MethodImplOptions.NoInlining)] public void Instance() { }

            [MethodImpl(MethodImplOptions.NoInlining)] // legacy JIT x64 was able to inline this method ;)
            public void Recursive()
            {
                if (new Random(123).Next(0, 10) == 11) // never true, but JIT does not know it
                    Recursive();
            }

            [MethodImpl(MethodImplOptions.NoInlining)] public void Benchmark(bool justAnOverload) { } // we need to test overloads (#562)
        }

        [Theory]
        [MemberData(nameof(GetAllJits))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleAllMethodCalls(Jit jit, Platform platform, Runtime runtime)
        {
            if (RuntimeInformation.IsMacOS()) return; // currently not supported

            var disassemblyDiagnoser = new DisassemblyDiagnoser(
                new DisassemblyDiagnoserConfig(printSource: true, maxDepth: 3));

            CanExecute<WithCalls>(CreateConfig(jit, platform, runtime, disassemblyDiagnoser, RunStrategy.ColdStart));

            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Benchmark)}(Int32)");
            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Benchmark)}(Boolean)");
            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Static)}()");
            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Instance)}()");
            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Recursive)}()");
        }

        [Theory]
        [MemberData(nameof(GetAllJits))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleAllMethodCallsUsingFilters(Jit jit, Platform platform, Runtime runtime)
        {
            if (RuntimeInformation.IsMacOS()) return; // currently not supported

            var disassemblyDiagnoser = new DisassemblyDiagnoser(
                new DisassemblyDiagnoserConfig(printSource: true, maxDepth: 1, filters: new[] { "*WithCalls*" }));

            CanExecute<WithCalls>(CreateConfig(jit, platform, runtime, disassemblyDiagnoser, RunStrategy.ColdStart));

            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Benchmark)}(Int32)");
            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Benchmark)}(Boolean)");
            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Static)}()");
            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Instance)}()");
            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Recursive)}()");
        }

        public class Generic<T> where T : new()
        {
            [Benchmark]
            public T Create() => new T();
        }

        [Theory]
        [MemberData(nameof(GetAllJits))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleGenericTypes(Jit jit, Platform platform, Runtime runtime)
        {
            if (RuntimeInformation.IsMacOS()) return; // currently not supported

            var disassemblyDiagnoser = new DisassemblyDiagnoser(
                new DisassemblyDiagnoserConfig(printSource: true, maxDepth: 3));

            CanExecute<Generic<int>>(CreateConfig(jit, platform, runtime, disassemblyDiagnoser, RunStrategy.Monitoring));

            var result = disassemblyDiagnoser.Results.Values.Single();

            Assert.Contains(result.Methods, method => method.Maps.Any(map => map.SourceCodes.OfType<Asm>().Any()));
        }

        public class WithInlineable
        {
            [Benchmark] public void JustReturn() { }
        }

        [Theory]
        [MemberData(nameof(GetAllJits))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleInlinableBenchmarks(Jit jit, Platform platform, Runtime runtime)
        {
            if (RuntimeInformation.IsMacOS()) return; // currently not supported

            var disassemblyDiagnoser = new DisassemblyDiagnoser(
                new DisassemblyDiagnoserConfig(printSource: true, maxDepth: 3));

            CanExecute<WithInlineable>(CreateConfig(jit, platform, runtime, disassemblyDiagnoser, RunStrategy.Monitoring));

            var disassemblyResult = disassemblyDiagnoser.Results.Values.Single(result => result.Methods.Count(method => method.Name.Contains(nameof(WithInlineable.JustReturn))) == 1);

            Assert.Contains(disassemblyResult.Methods, method => method.Maps.Any(map => map.SourceCodes.OfType<Asm>().All(asm => asm.ToString().Contains("ret"))));
        }

        private IConfig CreateConfig(Jit jit, Platform platform, Runtime runtime, IDiagnoser disassemblyDiagnoser, RunStrategy runStrategy)
            => ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithJit(jit)
                    .WithPlatform(platform)
                    .WithRuntime(runtime)
                    .WithStrategy(runStrategy))
                .AddLogger(DefaultConfig.Instance.GetLoggers().ToArray())
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddDiagnoser(disassemblyDiagnoser)
                .AddLogger(new OutputLogger(Output));

        private void AssertDisassembled(DisassemblyDiagnoser diagnoser, string methodSignature)
        {
            DisassemblyResult result = diagnoser.Results.Single().Value;

            Assert.Contains(methodSignature, result.Methods.Select(m => m.Name.Split('.').Last()).ToArray());
            Assert.Contains(result.Methods.Single(m => m.Name.EndsWith(methodSignature)).Maps, map => map.SourceCodes.Any());
        }
    }
}