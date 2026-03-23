using BenchmarkDotNet.Analyzers.General;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;
using System.Threading.Tasks;
using Xunit;

namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.General;

public class AsyncBenchmarkAnalyzerTests
{
    public class AsyncBenchmarkShouldHaveCancellationToken : AnalyzerTestFixture<AsyncBenchmarkAnalyzer>
    {
        public AsyncBenchmarkShouldHaveCancellationToken() : base(AsyncBenchmarkAnalyzer.AsyncBenchmarkShouldHaveCancellationTokenRule) { }

        [Fact]
        public async Task Async_benchmark_without_cancellation_token_should_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Threading.Tasks;

                public class BenchmarkClass
                {
                    [Benchmark]
                    public async Task {|#0:AsyncBenchmark|}()
                    {
                        await Task.Delay(100);
                    }
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, "BenchmarkClass");

            await RunAsync();
        }

        [Fact]
        public async Task Async_benchmark_with_cancellation_token_property_should_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Threading;
                using System.Threading.Tasks;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public CancellationToken CancellationToken { get; set; }

                    [Benchmark]
                    public async Task AsyncBenchmark()
                    {
                        await Task.Delay(100, CancellationToken);
                    }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Fact]
        public async Task Async_benchmark_with_cancellation_token_field_should_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Threading;
                using System.Threading.Tasks;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public CancellationToken cancellationToken;

                    [Benchmark]
                    public async Task AsyncBenchmark()
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Fact]
        public async Task Non_async_benchmark_should_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Threading.Tasks;

                public class BenchmarkClass
                {
                    [Benchmark]
                    public Task SyncBenchmark()
                    {
                        return Task.CompletedTask;
                    }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Fact]
        public async Task Class_without_benchmarks_should_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading.Tasks;

                public class RegularClass
                {
                    public async Task AsyncMethod()
                    {
                        await Task.Delay(100);
                    }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Fact]
        public async Task Multiple_async_benchmarks_without_cancellation_token_should_trigger_one_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Threading.Tasks;

                public class BenchmarkClass
                {
                    [Benchmark]
                    public async Task {|#0:AsyncBenchmark1|}()
                    {
                        await Task.Delay(100);
                    }

                    [Benchmark]
                    public async Task AsyncBenchmark2()
                    {
                        await Task.Delay(200);
                    }
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, "BenchmarkClass");

            await RunAsync();
        }

        [Fact]
        public async Task Mixed_async_and_sync_benchmarks_without_cancellation_token_should_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Threading.Tasks;

                public class BenchmarkClass
                {
                    [Benchmark]
                    public void SyncBenchmark()
                    {
                    }

                    [Benchmark]
                    public async Task {|#0:AsyncBenchmark|}()
                    {
                        await Task.Delay(100);
                    }
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, "BenchmarkClass");

            await RunAsync();
        }

        [Fact]
        public async Task Async_benchmark_with_inherited_cancellation_token_should_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Threading;
                using System.Threading.Tasks;

                public class BaseClass
                {
                    [BenchmarkCancellation]
                    public CancellationToken CancellationToken { get; set; }
                }

                public class BenchmarkClass : BaseClass
                {
                    [Benchmark]
                    public async Task AsyncBenchmark()
                    {
                        await Task.Delay(100, CancellationToken);
                    }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }
    }
}
