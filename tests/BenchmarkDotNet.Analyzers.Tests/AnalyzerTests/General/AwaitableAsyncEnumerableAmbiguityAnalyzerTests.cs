using BenchmarkDotNet.Analyzers.General;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;

namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.General;

public class AwaitableAsyncEnumerableAmbiguityAnalyzerTests
{
    public class AmbiguousReturnType : AnalyzerTestFixture<AwaitableAsyncEnumerableAmbiguityAnalyzer>
    {
        public AmbiguousReturnType() : base(AwaitableAsyncEnumerableAmbiguityAnalyzer.AmbiguousReturnTypeRule) { }

        // A user-defined type that satisfies BOTH the awaitable pattern (public parameterless GetAwaiter
        // returning an awaiter with IsCompleted/GetResult/OnCompleted) AND the async-enumerable pattern
        // (public GetAsyncEnumerator with all-optional args; enumerator with MoveNextAsync awaitable-to-bool
        // and Current property). The runtime resolution prefers the awaitable path. Uses fully qualified
        // names so the snippet can be appended after the class declaration without a stray `using`.
        private const string AwaitableAndAsyncEnumerableType = """
            public readonly struct DualShaped
            {
                public DualShapedAwaiter GetAwaiter() => default;
                public DualShapedEnumerator GetAsyncEnumerator() => default;
            }

            public readonly struct DualShapedAwaiter : System.Runtime.CompilerServices.INotifyCompletion
            {
                public bool IsCompleted => true;
                public int GetResult() => 0;
                public void OnCompleted(System.Action continuation) => continuation();
            }

            public struct DualShapedEnumerator
            {
                public int Current => 0;
                public DualShapedAwaitableBool MoveNextAsync() => default;
            }

            public readonly struct DualShapedAwaitableBool
            {
                public DualShapedBoolAwaiter GetAwaiter() => default;
            }

            public readonly struct DualShapedBoolAwaiter : System.Runtime.CompilerServices.INotifyCompletion
            {
                public bool IsCompleted => true;
                public bool GetResult() => false;
                public void OnCompleted(System.Action continuation) => continuation();
            }
            """;

        [Fact]
        public async Task Benchmark_returning_dual_shaped_type_should_trigger_diagnostic()
        {
            var testCode = $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    public {|#0:DualShaped|} Run() => default;
                }

                {{AwaitableAndAsyncEnumerableType}}
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, "Benchmark", "Run", "DualShaped");

            await RunAsync();
        }

        [Fact]
        public async Task GlobalSetup_returning_dual_shaped_type_should_trigger_diagnostic()
        {
            var testCode = $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [GlobalSetup]
                    public {|#0:DualShaped|} Setup() => default;

                    [Benchmark]
                    public void Benchmark() { }
                }

                {{AwaitableAndAsyncEnumerableType}}
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, "GlobalSetup", "Setup", "DualShaped");

            await RunAsync();
        }

        [Fact]
        public async Task IterationCleanup_returning_dual_shaped_type_should_trigger_diagnostic()
        {
            var testCode = $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [IterationCleanup]
                    public {|#0:DualShaped|} Cleanup() => default;

                    [Benchmark]
                    public void Benchmark() { }
                }

                {{AwaitableAndAsyncEnumerableType}}
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, "IterationCleanup", "Cleanup", "DualShaped");

            await RunAsync();
        }

        [Fact]
        public async Task Benchmark_returning_pure_awaitable_should_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Threading.Tasks;

                public class BenchmarkClass
                {
                    [Benchmark]
                    public async Task Run() => await Task.Yield();
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Fact]
        public async Task Benchmark_returning_pure_async_enumerable_should_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class BenchmarkClass
                {
                    [Benchmark]
                    public async IAsyncEnumerable<int> Producer()
                    {
                        await Task.Yield();
                        yield return 1;
                    }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Fact]
        public async Task Unannotated_method_returning_dual_shaped_type_should_not_trigger_diagnostic()
        {
            var testCode = $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public DualShaped NotABenchmark() => default;

                    [Benchmark]
                    public void Benchmark() { }
                }

                {{AwaitableAndAsyncEnumerableType}}
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Fact]
        public async Task Benchmark_returning_void_should_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }
    }
}
