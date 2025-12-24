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
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class DisassemblyDiagnoserTests : BenchmarkTestExecutor
    {
        public DisassemblyDiagnoserTests(ITestOutputHelper output) : base(output) { }

        public static IEnumerable<object[]> GetAllJits()
        {
            yield return [JitInfo.GetCurrentJit(), RuntimeInformation.GetCurrentPlatform(), InProcessEmitToolchain.Default]; // InProcess

            if (RuntimeInformation.IsFullFramework)
            {
                yield return [Jit.LegacyJit, Platform.X86, CsProjClassicNetToolchain.Net462]; // 32bit LegacyJit for desktop .NET
                yield return [Jit.LegacyJit, Platform.X64, CsProjClassicNetToolchain.Net462]; // 64bit LegacyJit for desktop .NET
                yield return [Jit.RyuJit, Platform.X64, CsProjClassicNetToolchain.Net462]; // RyuJit for desktop .NET
            }
            else if (RuntimeInformation.IsNetCore)
            {
                if (RuntimeInformation.GetCurrentPlatform() is Platform.X86 or Platform.X64)
                {
                    yield return [Jit.RyuJit, Platform.X64, CsProjCoreToolchain.NetCoreApp80]; // .NET Core x64
                    // We could add Platform.X86 here, but it would make our CI more complicated.
                }
                else if (RuntimeInformation.GetCurrentPlatform() is Platform.Arm64)
                {
                    yield return [Jit.RyuJit, Platform.Arm64, CsProjCoreToolchain.NetCoreApp80]; // .NET Core arm64
                }
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

        [TheoryEnvSpecific("Not supported on Windows+Arm", EnvRequirement.NonWindowsArm)]
        [MemberData(nameof(GetAllJits), DisableDiscoveryEnumeration = true)]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleAllMethodCalls(Jit jit, Platform platform, IToolchain toolchain)
        {
            var disassemblyDiagnoser = new DisassemblyDiagnoser(
                new DisassemblyDiagnoserConfig(printSource: true, maxDepth: 3));

            CanExecute<WithCalls>(CreateConfig(jit, platform, toolchain, disassemblyDiagnoser, RunStrategy.ColdStart));

            DisassemblyResult result = disassemblyDiagnoser.Results.Single().Value;

            Assert.Empty(result.Errors);
            AssertDisassemblyResult(result, $"{nameof(WithCalls.Benchmark)}(Int32)");
            AssertDisassemblyResult(result, $"{nameof(WithCalls.Benchmark)}(Boolean)");
            AssertDisassemblyResult(result, $"{nameof(WithCalls.Static)}()");
            AssertDisassemblyResult(result, $"{nameof(WithCalls.Instance)}()");
            AssertDisassemblyResult(result, $"{nameof(WithCalls.Recursive)}()");
        }

        [TheoryEnvSpecific("Not supported on Windows+Arm", EnvRequirement.NonWindowsArm)]
        [MemberData(nameof(GetAllJits), DisableDiscoveryEnumeration = true)]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleAllMethodCallsUsingFilters(Jit jit, Platform platform, IToolchain toolchain)
        {
            var disassemblyDiagnoser = new DisassemblyDiagnoser(
                new DisassemblyDiagnoserConfig(printSource: true, maxDepth: 1, filters: new[] { "*WithCalls*" }));

            CanExecute<WithCalls>(CreateConfig(jit, platform, toolchain, disassemblyDiagnoser, RunStrategy.ColdStart));

            DisassemblyResult result = disassemblyDiagnoser.Results.Single().Value;

            Assert.Empty(result.Errors);
            AssertDisassemblyResult(result, $"{nameof(WithCalls.Benchmark)}(Int32)");
            AssertDisassemblyResult(result, $"{nameof(WithCalls.Benchmark)}(Boolean)");
            AssertDisassemblyResult(result, $"{nameof(WithCalls.Static)}()");
            AssertDisassemblyResult(result, $"{nameof(WithCalls.Instance)}()");
            AssertDisassemblyResult(result, $"{nameof(WithCalls.Recursive)}()");
        }

        public class Generic<T> where T : new()
        {
            [Benchmark]
            public T Create() => new T();
        }

        [TheoryEnvSpecific("Not supported on Windows+Arm", EnvRequirement.NonWindowsArm)]
        [MemberData(nameof(GetAllJits), DisableDiscoveryEnumeration = true)]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleGenericTypes(Jit jit, Platform platform, IToolchain toolchain)
        {
            var disassemblyDiagnoser = new DisassemblyDiagnoser(
                new DisassemblyDiagnoserConfig(printSource: true, maxDepth: 3));

            CanExecute<Generic<int>>(CreateConfig(jit, platform, toolchain, disassemblyDiagnoser, RunStrategy.Monitoring));

            var result = disassemblyDiagnoser.Results.Values.Single();

            Assert.Empty(result.Errors);
            Assert.Contains(result.Methods, method => method.Maps.Any(map => map.SourceCodes.OfType<Asm>().Any()));
        }

        public class WithInlineable
        {
            [Benchmark] public void JustReturn() { }
        }

        [TheoryEnvSpecific("Not supported on Windows+Arm", EnvRequirement.NonWindowsArm)]
        [MemberData(nameof(GetAllJits), DisableDiscoveryEnumeration = true)]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleInlinableBenchmarks(Jit jit, Platform platform, IToolchain toolchain)
        {
            var disassemblyDiagnoser = new DisassemblyDiagnoser(
                new DisassemblyDiagnoserConfig(printSource: true, maxDepth: 3));

            CanExecute<WithInlineable>(CreateConfig(jit, platform, toolchain, disassemblyDiagnoser, RunStrategy.Monitoring));

            var disassemblyResult = disassemblyDiagnoser.Results.Values.Single(result => result.Methods.Count(method => method.Name.Contains(nameof(WithInlineable.JustReturn))) == 1);

            Assert.Empty(disassemblyResult.Errors);
            Assert.Contains(disassemblyResult.Methods, method => method.Maps.Any(map => map.SourceCodes.OfType<Asm>().All(asm => asm.ToString().Contains("ret"))));
        }

        private IConfig CreateConfig(Jit jit, Platform platform, IToolchain toolchain, IDiagnoser disassemblyDiagnoser, RunStrategy runStrategy)
            => ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithJit(jit)
                    .WithPlatform(platform)
                    .WithToolchain(toolchain)
                    .WithStrategy(runStrategy)
                    // Ensure the build goes through the full process and doesn't build without dependencies like most of the integration tests do.
#if RELEASE
                    .WithCustomBuildConfiguration("Release")
#else
                    .WithCustomBuildConfiguration("Debug")
#endif
                )
                .AddLogger(DefaultConfig.Instance.GetLoggers().ToArray())
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddDiagnoser(disassemblyDiagnoser)
                .AddLogger(new OutputLogger(Output));

        private void AssertDisassemblyResult(DisassemblyResult result, string methodSignature)
        {
            Assert.Contains(methodSignature, result.Methods.Select(m => m.Name.Split('.').Last()).ToArray());
            Assert.Contains(result.Methods.Single(m => m.Name.EndsWith(methodSignature)).Maps, map => map.SourceCodes.Any());
        }
    }
}