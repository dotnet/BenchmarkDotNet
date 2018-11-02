using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class DisassemblyDiagnoserTests : BenchmarkTestExecutor
    {
        private const string WindowsOnly = "Disassembler supports only Windows";

        public DisassemblyDiagnoserTests(ITestOutputHelper output) : base(output)
        {
        }

        public static IEnumerable<object[]> GetAllJits()
            => new[]
            {
#if CLASSIC
                new object[] { Jit.LegacyJit, Platform.X86, Runtime.Clr }, // 32bit LegacyJit for desktop .NET
                new object[] { Jit.LegacyJit, Platform.X64, Runtime.Clr }, // 64bit LegacyJit for desktop .NET

                new object[] { Jit.RyuJit, Platform.X64, Runtime.Clr }, // RyuJit for desktop .NET
#endif
                new object[] { Jit.RyuJit, Platform.X64, Runtime.Core }, // .NET Core

                // we could add new object[] { Jit.Llvm, Platform.X64, Runtime.Mono } here but our CI would need to have Mono installed..
            };

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
                Virtual();

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

            public virtual void Virtual() { }

            [MethodImpl(MethodImplOptions.NoInlining)] public void Benchmark(bool justAnOverload) { } // we need to test overloads (#562)
        }

        [TheoryWindowsOnly(WindowsOnly)]
        [MemberData(nameof(GetAllJits))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleAllMethodCalls(Jit jit, Platform platform, Runtime runtime)
        {
            var disassemblyDiagnoser = (IDisassemblyDiagnoser)DisassemblyDiagnoser.Create(
                new DisassemblyDiagnoserConfig(printAsm: true, printIL: true, printSource: true, recursiveDepth: 3));

            CanExecute<WithCalls>(CreateConfig(jit, platform, runtime, disassemblyDiagnoser, RunStrategy.ColdStart));

            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Benchmark)}(Int32)");
            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Benchmark)}(Boolean)");
            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Static)}()");
            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Instance)}()");
            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Recursive)}()");
            AssertDisassembled(disassemblyDiagnoser, $"{nameof(WithCalls.Virtual)}()");
        }

        public class Generic<T> where T : new()
        {
            [Benchmark]
            public T Create() => new T();
        }

        [TheoryWindowsOnly(WindowsOnly)]
        [MemberData(nameof(GetAllJits))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleGenericTypes(Jit jit, Platform platform, Runtime runtime)
        {
            var disassemblyDiagnoser = (IDisassemblyDiagnoser)DisassemblyDiagnoser.Create(
                new DisassemblyDiagnoserConfig(printAsm: true, printIL: true, printSource: true, recursiveDepth: 3));

            CanExecute<Generic<int>>(CreateConfig(jit, platform, runtime, disassemblyDiagnoser, RunStrategy.Monitoring));

            var result = disassemblyDiagnoser.Results.Values.Single();

            Assert.Contains(result.Methods, method => method.Maps.Any(map => map.Instructions.OfType<Asm>().Any()));
        }

        public class WithInlineable
        {
            [Benchmark] public void JustReturn() { }
        }
        
        [TheoryWindowsOnly(WindowsOnly)]
        [MemberData(nameof(GetAllJits))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void CanDisassembleInlinableBenchmarks(Jit jit, Platform platform, Runtime runtime)
        {
            var disassemblyDiagnoser = (IDisassemblyDiagnoser)DisassemblyDiagnoser.Create(
                new DisassemblyDiagnoserConfig(printAsm: true, printIL: true, printSource: true, recursiveDepth: 3));

            CanExecute<WithInlineable>(CreateConfig(jit, platform, runtime, disassemblyDiagnoser, RunStrategy.Monitoring));

            var disassemblyResult = disassemblyDiagnoser.Results.Values.Single(result => result.Methods.Count(method => method.Name.Contains(nameof(WithInlineable.JustReturn))) == 1);

            Assert.Contains(disassemblyResult.Methods, method => method.Maps.Any(map => map.Instructions.OfType<Asm>().All(asm => asm.TextRepresentation.Contains("ret"))));
        }

        private IConfig CreateConfig(Jit jit, Platform platform, Runtime runtime, IDiagnoser disassemblyDiagnoser, RunStrategy runStrategy)
            => ManualConfig.CreateEmpty()
                .With(Job.Dry.With(jit).With(platform).With(runtime).With(runStrategy))
                .With(DefaultConfig.Instance.GetLoggers().ToArray())
                .With(DefaultColumnProviders.Instance)
                .With(disassemblyDiagnoser)
                .With(new OutputLogger(Output));

        private void AssertDisassembled(IDisassemblyDiagnoser diagnoser, string methodSignature)
        {
            Assert.True(diagnoser.Results.Single().Value
                .Methods.Any(method => method.Name.EndsWith(methodSignature) && method.Maps.Any(map => map.Instructions.Any())),
                $"{methodSignature} is missing");
        }
    }
}