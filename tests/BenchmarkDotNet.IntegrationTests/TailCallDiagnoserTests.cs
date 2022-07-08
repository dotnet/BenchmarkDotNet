#if NETFRAMEWORK
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Tests.Loggers;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Tests.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class TailCallDiagnoserTests : BenchmarkTestExecutor
    {
        private const string WindowsOnly = "Use JIT ETW Tail Call Event (Windows only)";
        private const string TAIL_CALL_MARK = "Tail call type";

        public TailCallDiagnoserTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {

        }

        public static IEnumerable<object[]> GetJits()
            => new[]
            {
                new object[] { Jit.LegacyJit, Platform.X64, ClrRuntime.Net462 }, // 64bit LegacyJit for desktop .NET
                new object[] { Jit.RyuJit, Platform.X64, ClrRuntime.Net462 }, // RyuJit for desktop .NET
            };

        public class TailCallBenchmarks
        {
            private long FactorialWithTailing(int pos, int depth)
                => pos == 0 ? depth : FactorialWithTailing(pos - 1, depth * pos);

            private long FactorialWithTailing(int depth) => FactorialWithTailing(1, depth);

            [Benchmark]
            public long Factorial() => FactorialWithTailing(7);
        }

        public class NonTailCallBenchmarks
        {
            private long FactorialWithoutTailing(int depth) => depth == 0 ? 1 : depth * FactorialWithoutTailing(depth - 1);

            [Benchmark]
            public long Factorial() => FactorialWithoutTailing(7);
        }

        [TheoryWindowsOnly(WindowsOnly)]
        [MemberData(nameof(GetJits))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void TailCallDiagnoserCatchesTailCallEvents(Jit jit, Platform platform, Runtime runtime)
        {
            var output = Execute<TailCallBenchmarks>(jit, platform, runtime);

            Assert.Contains(output.CapturedOutput, x => x.Text.Contains(TAIL_CALL_MARK));
        }

        [TheoryWindowsOnly(WindowsOnly)]
        [MemberData(nameof(GetJits))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void TailCallDiagnoserNotCatchesTailCallEvents(Jit jit, Platform platform, Runtime runtime)
        {
            var output = Execute<NonTailCallBenchmarks>(jit, platform, runtime);

            Assert.DoesNotContain(output.CapturedOutput, x => x.Text.Contains(TAIL_CALL_MARK));
        }

        private LogCapture Execute<T>(Jit jit, Platform platform, Runtime runtime)
        {
            var tailCallDiagnoser = new TailCallDiagnoser(false, true);

            CanExecute<T>(CreateConfig(jit, platform, runtime, tailCallDiagnoser));

            return tailCallDiagnoser.Logger;
        }

        private IConfig CreateConfig(Jit jit, Platform platform, Runtime runtime, TailCallDiagnoser diagnoser) => ManualConfig.CreateEmpty()
            .AddJob(Job.Dry.WithJit(jit).WithPlatform(platform).WithRuntime(runtime))
            .AddLogger(DefaultConfig.Instance.GetLoggers().ToArray())
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .AddDiagnoser(diagnoser)
            .AddLogger(new OutputLogger(Output));
    }
}
#endif
