using BenchmarkDotNet.Analyzers.Attributes;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;

namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.Attributes;

public class SetupCleanupAsyncEnumerableAnalyzerTests
{
    public class MustNotReturnAsyncEnumerable : AnalyzerTestFixture<SetupCleanupAsyncEnumerableAnalyzer>
    {
        public MustNotReturnAsyncEnumerable() : base(SetupCleanupAsyncEnumerableAnalyzer.MustNotReturnAsyncEnumerableRule) { }

        [Fact]
        public async Task GlobalSetup_returning_IAsyncEnumerable_should_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class BenchmarkClass
                {
                    [GlobalSetup]
                    public async {|#0:IAsyncEnumerable<int>|} Setup()
                    {
                        await Task.Yield();
                        yield return 1;
                    }

                    [Benchmark]
                    public void Benchmark() { }
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, "GlobalSetup", "Setup");

            await RunAsync();
        }

        [Fact]
        public async Task GlobalCleanup_returning_IAsyncEnumerable_should_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class BenchmarkClass
                {
                    [GlobalCleanup]
                    public async {|#0:IAsyncEnumerable<int>|} Cleanup()
                    {
                        await Task.Yield();
                        yield return 1;
                    }

                    [Benchmark]
                    public void Benchmark() { }
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, "GlobalCleanup", "Cleanup");

            await RunAsync();
        }

        [Fact]
        public async Task IterationSetup_returning_IAsyncEnumerable_should_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class BenchmarkClass
                {
                    [IterationSetup]
                    public async {|#0:IAsyncEnumerable<int>|} Setup()
                    {
                        await Task.Yield();
                        yield return 1;
                    }

                    [Benchmark]
                    public void Benchmark() { }
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, "IterationSetup", "Setup");

            await RunAsync();
        }

        [Fact]
        public async Task IterationCleanup_returning_IAsyncEnumerable_should_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class BenchmarkClass
                {
                    [IterationCleanup]
                    public async {|#0:IAsyncEnumerable<int>|} Cleanup()
                    {
                        await Task.Yield();
                        yield return 1;
                    }

                    [Benchmark]
                    public void Benchmark() { }
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, "IterationCleanup", "Cleanup");

            await RunAsync();
        }

        [Fact]
        public async Task GlobalSetup_returning_pattern_async_enumerable_should_trigger_diagnostic()
        {
            // Custom struct that matches the `await foreach` pattern but doesn't implement IAsyncEnumerable<T>.
            // The runtime validator rejects this too (it uses the same pattern-first → interface-fallback
            // resolution as the C# compiler).
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System;
                using System.Runtime.CompilerServices;

                public class BenchmarkClass
                {
                    [GlobalSetup]
                    public {|#0:CustomAsyncEnumerable|} Setup() => default;

                    [Benchmark]
                    public void Benchmark() { }
                }

                public readonly struct CustomAsyncEnumerable
                {
                    public CustomAsyncEnumerator GetAsyncEnumerator() => default;
                }

                public struct CustomAsyncEnumerator
                {
                    public int Current => default;
                    public CustomBoolAwaitable MoveNextAsync() => default;
                }

                public readonly struct CustomBoolAwaitable
                {
                    public CustomBoolAwaiter GetAwaiter() => default;
                }

                public readonly struct CustomBoolAwaiter : INotifyCompletion
                {
                    public bool IsCompleted => true;
                    public bool GetResult() => false;
                    public void OnCompleted(Action continuation) => continuation();
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, "GlobalSetup", "Setup");

            await RunAsync();
        }

        [Fact]
        public async Task GlobalSetup_returning_Task_should_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Threading.Tasks;

                public class BenchmarkClass
                {
                    [GlobalSetup]
                    public async Task Setup() => await Task.Yield();

                    [Benchmark]
                    public void Benchmark() { }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Fact]
        public async Task GlobalSetup_returning_void_should_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [GlobalSetup]
                    public void Setup() { }

                    [Benchmark]
                    public void Benchmark() { }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Fact]
        public async Task Benchmark_method_returning_IAsyncEnumerable_should_not_trigger_diagnostic()
        {
            // The rule only targets [GlobalSetup]/[GlobalCleanup]/[IterationSetup]/[IterationCleanup].
            // [Benchmark] methods returning async enumerables are supported and run through the
            // AsyncEnumerable consumer path.
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
        public async Task Unannotated_method_returning_IAsyncEnumerable_should_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class BenchmarkClass
                {
                    public async IAsyncEnumerable<int> NotASetup()
                    {
                        await Task.Yield();
                        yield return 1;
                    }

                    [Benchmark]
                    public void Benchmark() { }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }
    }
}
