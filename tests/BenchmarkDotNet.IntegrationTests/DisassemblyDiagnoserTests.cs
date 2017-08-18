﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
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
                new object[] { Jit.LegacyJit, Platform.X86, Runtime.Clr }, // 32bit LegacyJit for desktop .NET
                new object[] { Jit.LegacyJit, Platform.X64, Runtime.Clr }, // 64bit LegacyJit for desktop .NET

                new object[] { Jit.RyuJit, Platform.X64, Runtime.Clr }, // RyuJit for desktop .NET

                new object[] { Jit.RyuJit, Platform.X64, Runtime.Core }, // .NET Core

                // we could add new object[] { Jit.Llvm, Platform.X64, Runtime.Mono } here but our CI would need to have Mono installed..
            };

        public class WithCalls
        {
            [Benchmark]
            public void Benchmark()
            {
                // we should rather have test per use case
                // but running so many tests for all JITs would take too much time
                // so we have one method that does it all
                Static();
                Instance();
                Recursive();
                Virtual();
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
        }

#if CORE
        [Theory(Skip = "Disassembler has not .NET Core support yet")]
#else
        [Theory]
#endif
        [MemberData(nameof(GetAllJits))]
        public void CanDisassembleAllMethodCalls(Jit jit, Platform platform, Runtime runtime)
        {
            var disassemblyDiagnoser = (IDisassemblyDiagnoser)DisassemblyDiagnoser.Create(
                new DisassemblyDiagnoserConfig(printAsm: true, printIL: true, printSource: true, recursiveDepth: 3));

            CanExecute<WithCalls>(CreateConfig(jit, platform, runtime, disassemblyDiagnoser));

            void AssertDisassembled(IDisassemblyDiagnoser diagnoser, string calledMethodName)
            {
                Assert.True(diagnoser.Results.Single().Value
                    .Methods.Any(method => method.Name.Contains(calledMethodName) && method.Maps.Any(map => map.Instructions.Any())),
                    $"{calledMethodName} is missing");
            }

            AssertDisassembled(disassemblyDiagnoser, nameof(WithCalls.Benchmark));
            AssertDisassembled(disassemblyDiagnoser, nameof(WithCalls.Static));
            AssertDisassembled(disassemblyDiagnoser, nameof(WithCalls.Instance));
            AssertDisassembled(disassemblyDiagnoser, nameof(WithCalls.Recursive));
            AssertDisassembled(disassemblyDiagnoser, nameof(WithCalls.Virtual));
        }

        private IConfig CreateConfig(Jit jit, Platform platform, Runtime runtime, IDiagnoser disassemblyDiagnoser)
            => ManualConfig.CreateEmpty()
                .With(Job.ShortRun.With(jit).With(platform).With(runtime))
                .With(DefaultConfig.Instance.GetLoggers().ToArray())
                .With(DefaultColumnProviders.Instance)
                .With(disassemblyDiagnoser)
                .With(new OutputLogger(Output));
    }
}