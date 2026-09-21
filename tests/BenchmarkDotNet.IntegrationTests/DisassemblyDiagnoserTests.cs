using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Disassemblers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Framework;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.NetCoreApp;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.IntegrationTests
{
    public class DisassemblyDiagnoserTests : BenchmarkTestExecutor
    {
        public DisassemblyDiagnoserTests(ITestOutputHelper output) : base(output) { }

        public static IEnumerable<object[]> GetAllJits()
        {
            // In-process disassembly dumps its own process, which hangs on macOS when it runs from an apphost as xUnit v3 requires.
            // https://github.com/dotnet/BenchmarkDotNet/issues/3076
            if (!OsDetector.IsMacOS())
                yield return [JitInfo.GetCurrentJit(), RuntimeInformation.GetCurrentPlatform(), InProcessEmitToolchain.Default]; // InProcess

            if (ContinuousIntegration.IsGitHubDraftPR())
                yield break;

            if (RuntimeInformation.IsFullFramework)
            {
                if (RuntimeInformation.GetCurrentPlatform() is Platform.Arm64)
                {
                    // RyuJit for desktop .NET arm64. Supported only on net481, but we have to match the tfm in our test project.
                    yield return [Jit.RyuJit, Platform.Arm64, CsProjFrameworkToolchain.Net472];
                }
                // Framework on arm emulates x86, so these platform targets should work on both.
                yield return [Jit.LegacyJit, Platform.X86, CsProjFrameworkToolchain.Net472]; // 32bit LegacyJit for desktop .NET
                yield return [Jit.LegacyJit, Platform.X64, CsProjFrameworkToolchain.Net472]; // 64bit LegacyJit for desktop .NET
                yield return [Jit.RyuJit, Platform.X64, CsProjFrameworkToolchain.Net472]; // RyuJit for desktop .NET
            }
            else if (RuntimeInformation.IsNetCore)
            {
                // Skip test on `macos(x64)` because test randomly failed on CI.
                // See: https://github.com/dotnet/BenchmarkDotNet/issues/3086
                if (RuntimeInformation.GetCurrentPlatform() is Platform.X86 or Platform.X64 && !OsDetector.IsMacOS())
                {
                    yield return [Jit.RyuJit, Platform.X64, CsProjCoreToolchain.NetCoreApp10_0]; // .NET Core x64
                    // We could add Platform.X86 here, but it would make our CI more complicated.
                }
                else if (RuntimeInformation.GetCurrentPlatform() is Platform.Arm64)
                {
                    yield return [Jit.RyuJit, Platform.Arm64, CsProjCoreToolchain.NetCoreApp10_0]; // .NET Core arm64
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

        [Theory(SkipTestWithoutData = true)]
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

        [Theory(SkipTestWithoutData = true)]
        [MemberData(nameof(GetAllJits), DisableDiscoveryEnumeration = true)]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleAllMethodCallsUsingFilters(Jit jit, Platform platform, IToolchain toolchain)
        {
            if (OsDetector.IsMacOS())
                Assert.Skip("https://github.com/dotnet/BenchmarkDotNet/issues/3076");

            var disassemblyDiagnoser = new DisassemblyDiagnoser(
                new DisassemblyDiagnoserConfig(printSource: true, maxDepth: 1, filters: ["*WithCalls*"]));

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

        [Theory(SkipTestWithoutData = true)]
        [MemberData(nameof(GetAllJits), DisableDiscoveryEnumeration = true)]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleGenericTypes(Jit jit, Platform platform, IToolchain toolchain)
        {
            if (OsDetector.IsMacOS())
                Assert.Skip("https://github.com/dotnet/BenchmarkDotNet/issues/3076");

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

        [Theory(SkipTestWithoutData = true)]
        [MemberData(nameof(GetAllJits), DisableDiscoveryEnumeration = true)]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleInlinableBenchmarks(Jit jit, Platform platform, IToolchain toolchain)
        {
            if (OsDetector.IsMacOS())
                Assert.Skip("https://github.com/dotnet/BenchmarkDotNet/issues/3076");

            var disassemblyDiagnoser = new DisassemblyDiagnoser(
                new DisassemblyDiagnoserConfig(printSource: true, maxDepth: 3));

            CanExecute<WithInlineable>(CreateConfig(jit, platform, toolchain, disassemblyDiagnoser, RunStrategy.Monitoring));

            var disassemblyResult = disassemblyDiagnoser.Results.Values.Single(result => result.Methods.Count(method => method.Name.Contains(nameof(WithInlineable.JustReturn))) == 1);

            Assert.Empty(disassemblyResult.Errors);
            Assert.Contains(disassemblyResult.Methods, method => method.Maps.Any(map => map.SourceCodes.OfType<Asm>().All(asm => asm.ToString()!.Contains("ret"))));
        }

        // The benchmark declares the names the generator gives its disassembly entry point and JIT-trick method, so both
        // are renamed on the runnable. The disassembler resolves its entry point by name and has to follow the rename.
        public class WithGeneratedMemberNames
        {
            [Benchmark]
            public void Benchmark() => Called();

            [MethodImpl(MethodImplOptions.NoInlining)] public void Called() { }

            // Deliberately a different signature from the generated method, which is what would otherwise make the
            // by-name reflection lookup of the JIT-trick method ambiguous.
            [MethodImpl(MethodImplOptions.NoInlining)] protected void TrickTheJIT(int notEleven) { }

            // Virtual on purpose: an inherited virtual method occupies a slot in the runnable's method table, which is the
            // case most likely to show up as a second match when the disassembler looks its entry point up by name.
            [MethodImpl(MethodImplOptions.NoInlining)] public virtual void ForDisassemblyDiagnoser() { }
        }

        [Theory(SkipTestWithoutData = true)]
        [MemberData(nameof(GetAllJits), DisableDiscoveryEnumeration = true)]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleWhenBenchmarkDeclaresGeneratedMemberNames(Jit jit, Platform platform, IToolchain toolchain)
        {
            var disassemblyDiagnoser = new DisassemblyDiagnoser(
                new DisassemblyDiagnoserConfig(printSource: true, maxDepth: 3));

            CanExecute<WithGeneratedMemberNames>(CreateConfig(jit, platform, toolchain, disassemblyDiagnoser, RunStrategy.ColdStart));

            DisassemblyResult result = disassemblyDiagnoser.Results.Single().Value;

            Assert.Empty(result.Errors);
            AssertDisassemblyResult(result, $"{nameof(WithGeneratedMemberNames.Benchmark)}()");
            AssertDisassemblyResult(result, $"{nameof(WithGeneratedMemberNames.Called)}()");
        }

        public class WithTwoBenchmarks
        {
            [Benchmark] public void First() => FirstCallee();

            [Benchmark] public void Second() => SecondCallee();

            [MethodImpl(MethodImplOptions.NoInlining)] public void FirstCallee() { }

            [MethodImpl(MethodImplOptions.NoInlining)] public void SecondCallee() { }
        }

        // Every benchmark of an in-process run is emitted into the same assembly, one runnable type each, so the disassembler
        // has to be pointed at the runnable of the benchmark it is handling rather than the first one.
        [Fact]
        public void InProcessDisassemblyTargetsTheRunnableOfEachBenchmark()
        {
            if (OsDetector.IsMacOS())
                Assert.Skip("https://github.com/dotnet/BenchmarkDotNet/issues/3076");

            var disassemblyDiagnoser = new DisassemblyDiagnoser(
                new DisassemblyDiagnoserConfig(printSource: true, maxDepth: 3));

            CanExecute<WithTwoBenchmarks>(CreateInProcessConfig(disassemblyDiagnoser));

            Assert.Equal(2, disassemblyDiagnoser.Results.Count);
            foreach (var pair in disassemblyDiagnoser.Results)
            {
                var benchmarkCase = pair.Key;
                var result = pair.Value;
                Assert.Empty(result.Errors);
                string called = benchmarkCase.Descriptor.WorkloadMethod.Name == nameof(WithTwoBenchmarks.First)
                    ? nameof(WithTwoBenchmarks.FirstCallee)
                    : nameof(WithTwoBenchmarks.SecondCallee);
                AssertDisassemblyResult(result, $"{benchmarkCase.Descriptor.WorkloadMethod.Name}()");
                AssertDisassemblyResult(result, $"{called}()");
            }
        }

        // Each in-process run emits its runnables under the same type names, and the assemblies of earlier runs can still be
        // loaded (never collected when saved to disk, which KeepBenchmarkFiles does on .NET Framework).
        [Fact]
        public void InProcessDisassemblyIgnoresRunnablesOfEarlierRuns()
        {
            if (OsDetector.IsMacOS())
                Assert.Skip("https://github.com/dotnet/BenchmarkDotNet/issues/3076");

            CanExecute<WithCalls>(ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default))
                .AddLogger(new OutputLogger(Output))
                .WithOptions(ConfigOptions.KeepBenchmarkFiles));

            var disassemblyDiagnoser = new DisassemblyDiagnoser(
                new DisassemblyDiagnoserConfig(printSource: true, maxDepth: 3));

            CanExecute<WithGeneratedMemberNames>(CreateInProcessConfig(disassemblyDiagnoser));

            DisassemblyResult result = disassemblyDiagnoser.Results.Single().Value;

            Assert.Empty(result.Errors);
            AssertDisassemblyResult(result, $"{nameof(WithGeneratedMemberNames.Benchmark)}()");
            AssertDisassemblyResult(result, $"{nameof(WithGeneratedMemberNames.Called)}()");
        }

        private IConfig CreateInProcessConfig(IDiagnoser disassemblyDiagnoser)
            => CreateConfig(JitInfo.GetCurrentJit(), RuntimeInformation.GetCurrentPlatform(), InProcessEmitToolchain.Default, disassemblyDiagnoser, RunStrategy.ColdStart);

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