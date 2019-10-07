﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(MultipleJits))]
    public class IntroDisassemblyAllJits
    {
        public class MultipleJits : ManualConfig
        {
            public MultipleJits()
            {
                AddJob(Job.ShortRun.WithPlatform(Platform.X86).WithRuntime(new MonoRuntime(name: "Mono x86", customPath: @"C:\Program Files (x86)\Mono\bin\mono.exe")));
                AddJob(Job.ShortRun.WithPlatform(Platform.X64).WithRuntime(new MonoRuntime(name: "Mono x64", customPath: @"C:\Program Files\Mono\bin\mono.exe")));

                AddJob(Job.ShortRun.WithJit(Jit.LegacyJit).WithPlatform(Platform.X86).WithRuntime(ClrRuntime.Net461));
                AddJob(Job.ShortRun.WithJit(Jit.LegacyJit).WithPlatform(Platform.X64).WithRuntime(ClrRuntime.Net461));

                AddJob(Job.ShortRun.WithJit(Jit.RyuJit).WithPlatform(Platform.X64).WithRuntime(ClrRuntime.Net461));

                // RyuJit for .NET Core 2.0
                AddJob(Job.ShortRun.WithJit(Jit.RyuJit).WithPlatform(Platform.X64).WithRuntime(CoreRuntime.Core20));

                // RyuJit for .NET Core 2.1
                AddJob(Job.ShortRun.WithJit(Jit.RyuJit).WithPlatform(Platform.X64).WithRuntime(CoreRuntime.Core21));

                AddDiagnoser(DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig(printAsm: true, printPrologAndEpilog: true, recursiveDepth: 3, printDiff: true)));
            }
        }

        private Increment increment = new Increment();

        [Benchmark]
        public int CallVirtualMethod() => increment.OperateTwice(10);

        public abstract class Operation  // abstract unary integer operation
        {
            public abstract int Operate(int input);

            public int OperateTwice(int input) => Operate(Operate(input)); // two virtual calls to Operate
        }

        public sealed class Increment : Operation // concrete, sealed operation: increment by fixed amount
        {
            public readonly int Amount;
            public Increment(int amount = 1) { Amount = amount; }

            public override int Operate(int input) => input + Amount;
        }
    }
}