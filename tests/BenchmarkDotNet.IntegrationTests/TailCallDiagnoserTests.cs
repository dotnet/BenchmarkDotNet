using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
#if NET46
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
#if NET46
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

        [TheoryWindowsOnly(WindowsOnly)]
        [MemberData(nameof(GetJits))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void TailCallDiagnoserCatchesTailCallEvents(Jit jit, Platform platform, Runtime runtime)
        {
            var output = Execute<TailCallBenchmarks>(jit, platform, runtime);
            Assert.True(output.CapturedOutput.Where(x=>x.Text.Contains(TAIL_CALL_MARK)).Count() > 0);
        }

        [TheoryWindowsOnly(WindowsOnly)]
        [MemberData(nameof(GetJits))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void TailCallDiagnoserNotCatchesTailCallEvents(Jit jit, Platform platform, Runtime runtime)
        {
            var output = Execute<NonTailCallBenchmarks>(jit, platform, runtime);
            Assert.True(output.CapturedOutput.Where(x => x.Text.Contains(TAIL_CALL_MARK)).Count() == 0);
        }

        private LogCapture Execute<T>(Jit jit, Platform platform, Runtime runtime)
        {
            var tailCallDiagnoser = new TailCallDiagnoser(false, true);
            CanExecute<T>(CreateConfig(jit, platform, runtime, tailCallDiagnoser));
            var output = (LogCapture)(tailCallDiagnoser as EtwDiagnoser<object>).GetType().GetField("Logger", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tailCallDiagnoser);
            return output;
        }

        private IConfig CreateConfig(Jit jit, Platform platform, Runtime runtime, TailCallDiagnoser diagnoser) => ManualConfig.CreateEmpty()
            .With(Job.Dry.With(jit).With(platform).With(runtime))
            .With(DefaultConfig.Instance.GetLoggers().ToArray())
            .With(DefaultColumnProviders.Instance)
            .With(diagnoser)
            .With(new OutputLogger(Output));
    }
#endif
}
