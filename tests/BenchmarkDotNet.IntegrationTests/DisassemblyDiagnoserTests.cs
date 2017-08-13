#if CLASSIC
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class DisassemblyDiagnoserTests : BenchmarkTestExecutor
    {
        public DisassemblyDiagnoserTests(ITestOutputHelper output) : base(output)
        {
        }

        public static IEnumerable<object[]> GetAllJits()
            => new[]
            {
                new object[] { Jit.LegacyJit, Platform.X86 },
                new object[] { Jit.LegacyJit, Platform.X64 },
                new object[] { Jit.RyuJit, Platform.X64 }
            };

        public class StaticMethodCall
        {
            [Benchmark] public void Benchmark() => StaticMethod();
            [MethodImpl(MethodImplOptions.NoInlining)] public static void StaticMethod() { }
        }

        [Theory, MemberData(nameof(GetAllJits))]
        public void CanDisassembleStaticMethodCall(Jit jit, Platform platform)
            => Test<StaticMethodCall>(jit, platform, nameof(StaticMethodCall.Benchmark), nameof(StaticMethodCall.StaticMethod));

        public class InstanceMethodCall
        {
            [Benchmark] public void Benchmark() => InsanceMethod();
            [MethodImpl(MethodImplOptions.NoInlining)] public void InsanceMethod() { }
        }

        [Theory, MemberData(nameof(GetAllJits))]
        public void CanDisassembleInstanceMethodCall(Jit jit, Platform platform)
            => Test<InstanceMethodCall>(jit, platform, nameof(InstanceMethodCall.Benchmark), nameof(InstanceMethodCall.InsanceMethod));

        public class RecursiveMethodCall
        {
            [Benchmark] public void Benchmark() => Recursive();

            public void Recursive()
            {
                if (new Random(123).Next(0, 10) == 11) // never true, but JIT does not know it
                    Recursive();
            }
        }

        [Theory, MemberData(nameof(GetAllJits))]
        public void CanDisassembleRecursiveMethodCall(Jit jit, Platform platform)
            => Test<RecursiveMethodCall>(jit, platform, nameof(RecursiveMethodCall.Benchmark), nameof(RecursiveMethodCall.Recursive));

        private void Test<TBenchmark>(Jit jit, Platform platform, string benchmarkName, string calledMethodName)
        {
            var disassemblyDiagnoser = new Diagnostics.Windows.DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(printAsm: true, recursiveDepth: 3));

            CanExecute<TBenchmark>(CreateConfig(jit, platform, disassemblyDiagnoser));

            AssertMethodsDisassembled(disassemblyDiagnoser, benchmarkName, calledMethodName);
        }

        private void AssertMethodsDisassembled(Diagnostics.Windows.DisassemblyDiagnoser disassemblyDiagnoser, string benchmarkName, string calledMethodName)
        {
            string output = disassemblyDiagnoser.GetOutput();

            Assert.NotEmpty(output);

            Assert.Contains(benchmarkName, output);
            Assert.Contains(calledMethodName, output);
        }

        private IConfig CreateConfig(Jit jit, Platform platform, IDiagnoser disassemblyDiagnoser)
        {
            return ManualConfig.CreateEmpty()
                .With(Job.ShortRun.With(jit).With(platform))
                .With(DefaultConfig.Instance.GetLoggers().ToArray())
                .With(DefaultColumnProviders.Instance)
                .With(disassemblyDiagnoser)
                .With(new OutputLogger(Output));
        }
    }
}
#endif