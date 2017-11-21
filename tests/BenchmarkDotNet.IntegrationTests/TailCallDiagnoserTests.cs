using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
#if !CORE
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Environments;
#endif
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
#if !CORE
    public class TailCallDiagnoserTests : BenchmarkTestExecutor
    {
        private readonly ITestOutputHelper output;

        public TailCallDiagnoserTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {

        }

        public static IEnumerable<object[]> GetJits()
            => new[]
            {
                new object[] { Jit.LegacyJit, Platform.X64, Runtime.Clr }, // 64bit LegacyJit for desktop .NET
                new object[] { Jit.RyuJit, Platform.X64, Runtime.Clr }, // RyuJit for desktop .NET
            };

        public class TailCallBenchmarks
        {
            private long FactorialWithTailing(int pos, int depth)
            {
                return pos == 0 ? depth : FactorialWithTailing(pos - 1, depth * pos);
            }

            private long FactorialWithTailing(int depth)
            {
                return FactorialWithTailing(1, depth);
            }
            
            [Benchmark]
            public long Factorial()
            {
                return FactorialWithTailing(7);
            }
        }
        
        public class NonTailCallBenchmarks
        {
            private long FactorialWithoutTailing(int depth)
            {
                return depth == 0 ? 1 : depth * FactorialWithoutTailing(depth - 1);
            }

            [Benchmark]
            public long Factorial()
            {
                return FactorialWithoutTailing(7);
            }
        }
        
        [Theory, MemberData(nameof(GetJits))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void TailCallDiagnoserCatchesTailCallEvents(Jit jit, Platform platform, Runtime runtime)
        {
            var tailCallDiagnoser = new TailCallDiagnoser(false, true);
            CanExecute<TailCallBenchmarks>(CreateConfig(jit, platform, runtime, tailCallDiagnoser));
            var output = (LogCapture)(tailCallDiagnoser as EtwDiagnoser<object>).GetType().GetField("Logger", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tailCallDiagnoser);
            Assert.True(output.CapturedOutput.Any(x => x.Text.Contains("Tail call type")));
        }

        [Theory, MemberData(nameof(GetJits))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void TailCallDiagnoserNotCatchesTailCallEvents(Jit jit, Platform platform, Runtime runtime)
        {
            var tailCallDiagnoser = new TailCallDiagnoser(false, true);
            CanExecute<NonTailCallBenchmarks>(CreateConfig(jit, platform, runtime, tailCallDiagnoser));
            var output = (LogCapture)(tailCallDiagnoser as EtwDiagnoser<object>).GetType().GetField("Logger", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tailCallDiagnoser);
            Assert.True(!output.CapturedOutput.Any(x => x.Text.Contains("Tail call type")));
        }

        private IConfig CreateConfig(Jit jit, Platform platform, Runtime runtime, TailCallDiagnoser diagnoser) => ManualConfig.CreateEmpty()
            .With(Job.ShortRun.With(jit).With(platform).With(runtime))
            .With(DefaultConfig.Instance.GetLoggers().ToArray())
            .With(DefaultColumnProviders.Instance)
            .With(diagnoser)
            .With(new OutputLogger(Output));
    }
#endif
}
